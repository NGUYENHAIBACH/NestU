using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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

    public AuthService(UserManager<ApplicationUser> userManager, NestUDbContext db, JwtTokenGenerator jwt)
    {
        _userManager = userManager;
        _db = db;
        _jwt = jwt;
    }

    public async Task<UserDto> RegisterAsync(RegisterDto dto)
    {
        if (await _userManager.FindByEmailAsync(dto.Email) is not null)
            throw ApiException.Conflict("Email đã được đăng ký.");

        if (dto.Role == Roles.Tenant && dto.IsStudent && dto.UniversityId is not null
            && !await _db.Universities.AnyAsync(u => u.Id == dto.UniversityId))
            throw ApiException.BadRequest("Trường đại học không tồn tại.");

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
                StudentCode = dto.IsStudent ? dto.StudentCode : null,
                UniversityId = dto.IsStudent ? dto.UniversityId : null
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
        var user = await _userManager.FindByEmailAsync(dto.Email);
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
