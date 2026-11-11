using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using NestU.API.Data;
using NestU.API.DTOs.Auth;
using NestU.API.Entities;
using NestU.API.Entities.Enums;
using NestU.API.Helpers;
using NestU.API.Services.Interfaces;

namespace NestU.API.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly NestUDbContext _db;
    private readonly JwtTokenGenerator _jwt;
    private readonly IMemoryCache _cache;
    private readonly IEmailService _email;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        NestUDbContext db,
        JwtTokenGenerator jwt,
        IMemoryCache cache,
        IEmailService email)
    {
        _userManager = userManager;
        _db = db;
        _jwt = jwt;
        _cache = cache;
        _email = email;
    }

    public async Task<UserDto> RegisterAsync(RegisterDto dto)
    {
        if (await _userManager.FindByEmailAsync(dto.Email) is not null)
            throw ApiException.Conflict("Email đã được đăng ký.");

        University? university = null;
        if (dto.Role == Roles.Tenant && dto.IsStudent)
        {
            if (string.IsNullOrWhiteSpace(dto.StudentCode))
                throw ApiException.BadRequest("Sinh viên phải nhập mã số sinh viên.");
            university = await Universities.ResolveAsync(_db, dto.UniversityId, dto.UniversityName);
        }

        await CitizenIds.EnsureNotUsedByOtherAsync(_db, dto.CitizenId, userId: null);
        await PhoneNumbers.EnsureNotUsedByOtherAsync(_db, dto.Phone, userId: null);

        await using var tx = await _db.Database.BeginTransactionAsync();

        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FullName = dto.FullName.Trim(),
            PhoneNumber = dto.Phone
        };
        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
            throw ApiException.BadRequest(string.Join(" ", result.Errors.Select(e => TranslateIdentityError(e))));

        await _userManager.AddToRoleAsync(user, dto.Role);

        if (dto.Role == Roles.Tenant)
        {
            _db.TenantProfiles.Add(new TenantProfile
            {
                UserId = user.Id,
                FullName = user.FullName,
                CitizenId = dto.CitizenId,
                Phone = dto.Phone,
                IsStudent = dto.IsStudent,
                StudentCode = dto.IsStudent ? dto.StudentCode?.Trim() : null,
                University = dto.IsStudent ? university : null
            });
        }
        else
        {
            _db.LandlordProfiles.Add(new LandlordProfile
            {
                UserId = user.Id,
                FullName = user.FullName,
                CitizenId = dto.CitizenId,
                Phone = dto.Phone,
                ApprovalStatus = ApprovalStatus.Pending
            });
        }

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return await GetMeAsync(user.Id);
    }

    public async Task<TokenDto> LoginAsync(LoginDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null || !await _userManager.CheckPasswordAsync(user, dto.Password))
            throw new ApiException(StatusCodes.Status401Unauthorized, "Email hoặc mật khẩu không đúng.");
        if (user.IsLocked)
            throw ApiException.Forbidden("Tài khoản đã bị khóa. Vui lòng liên hệ quản trị viên.");

        var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? Roles.Tenant;
        var (token, expires) = _jwt.Generate(user, role);

        string? approval = null;
        if (role == Roles.Landlord)
            approval = (await _db.LandlordProfiles.AsNoTracking()
                .Where(p => p.UserId == user.Id).Select(p => p.ApprovalStatus).FirstOrDefaultAsync()).ToString();

        return new TokenDto
        {
            AccessToken = token,
            ExpiresAt = expires,
            UserId = user.Id,
            Email = user.Email!,
            FullName = user.FullName,
            Role = role,
            LandlordApprovalStatus = approval
        };
    }

    public async Task<UserDto> GetMeAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId) ?? throw ApiException.NotFound("Không tìm thấy người dùng.");
        var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? string.Empty;

        var dto = new UserDto
        {
            Id = user.Id,
            Email = user.Email!,
            FullName = user.FullName,
            Role = role,
            IsLocked = user.IsLocked
        };

        if (role == Roles.Landlord)
            dto.LandlordApprovalStatus = (await _db.LandlordProfiles.AsNoTracking()
                .Where(p => p.UserId == userId).Select(p => p.ApprovalStatus).FirstOrDefaultAsync()).ToString();
        if (role == Roles.Tenant)
            dto.IsStudent = await _db.TenantProfiles.AsNoTracking()
                .Where(p => p.UserId == userId).Select(p => p.IsStudent).FirstOrDefaultAsync();

        return dto;
    }

    private const int MaxOtpAttempts = 5;

    private sealed class OtpEntry
    {
        private int _failures;
        public OtpEntry(string code) => Code = code;
        public string Code { get; }
        public int RegisterFailure() => Interlocked.Increment(ref _failures);
    }

    public async Task ForgotPasswordAsync(ForgotPasswordDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
            throw ApiException.BadRequest("Email này chưa được đăng ký tài khoản trong hệ thống.");

        if (user.IsLocked)
            throw ApiException.Forbidden("Tài khoản này đã bị khóa. Vui lòng liên hệ quản trị viên.");

        // Chống gửi mã liên tục (mỗi mã mới lại có thêm lượt thử, và mỗi lần gửi là một email thật)
        var cooldownKey = $"pwd_otp_cooldown_{email}";
        if (_cache.TryGetValue(cooldownKey, out _))
            throw ApiException.BadRequest("Bạn vừa yêu cầu mã OTP. Vui lòng đợi 1 phút rồi thử lại.");
        _cache.Set(cooldownKey, true, TimeSpan.FromMinutes(1));

        // Sinh mã OTP 6 chữ số ngẫu nhiên
        var otp = System.Security.Cryptography.RandomNumberGenerator.GetInt32(100000, 1000000).ToString("D6");
        var cacheKey = $"pwd_otp_{email}";

        // Lưu mã vào bộ nhớ đệm hiệu lực 5 phút
        _cache.Set(cacheKey, new OtpEntry(otp), TimeSpan.FromMinutes(5));

        // Gửi email
        await _email.SendPasswordResetOtpAsync(user.Email!, otp);
    }

    public async Task ResetPasswordAsync(ResetPasswordDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        var cacheKey = $"pwd_otp_{email}";

        if (!_cache.TryGetValue<OtpEntry>(cacheKey, out var entry) || entry is null)
            throw ApiException.BadRequest("Mã OTP không chính xác hoặc đã hết hạn (mã chỉ có hiệu lực trong 5 phút).");
        if (entry.Code != dto.Otp.Trim())
        {
            if (entry.RegisterFailure() >= MaxOtpAttempts)
            {
                _cache.Remove(cacheKey);
                throw ApiException.BadRequest($"Bạn đã nhập sai mã OTP {MaxOtpAttempts} lần. Mã đã bị hủy, vui lòng yêu cầu mã mới.");
            }
            throw ApiException.BadRequest("Mã OTP không chính xác hoặc đã hết hạn (mã chỉ có hiệu lực trong 5 phút).");
        }

        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
            throw ApiException.NotFound("Không tìm thấy tài khoản người dùng.");

        if (user.IsLocked)
            throw ApiException.Forbidden("Tài khoản này đã bị khóa. Vui lòng liên hệ quản trị viên.");

        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, resetToken, dto.NewPassword);
        if (!result.Succeeded)
            throw ApiException.BadRequest(string.Join(" ", result.Errors.Select(e => TranslateIdentityError(e))));

        _cache.Remove(cacheKey);
    }

    private static string TranslateIdentityError(IdentityError e) => e.Code switch
    {
        "PasswordTooShort" => "Mật khẩu quá ngắn.",
        "PasswordRequiresNonAlphanumeric" => "Mật khẩu cần ít nhất một ký tự đặc biệt.",
        "PasswordRequiresDigit" => "Mật khẩu cần ít nhất một chữ số.",
        "PasswordRequiresUpper" => "Mật khẩu cần ít nhất một chữ hoa.",
        "PasswordRequiresLower" => "Mật khẩu cần ít nhất một chữ thường.",
        "DuplicateUserName" or "DuplicateEmail" => "Email đã được đăng ký.",
        "InvalidEmail" => "Email không hợp lệ.",
        _ => e.Description
    };
}
