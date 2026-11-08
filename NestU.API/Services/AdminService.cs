using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NestU.API.Data;
using NestU.API.DTOs.Admin;
using NestU.API.DTOs.Common;
using NestU.API.DTOs.Houses;
using NestU.API.Entities;
using NestU.API.Entities.Enums;
using NestU.API.Helpers;
using NestU.API.Services.Interfaces;

namespace NestU.API.Services;

public class AdminService : IAdminService
{
    private readonly NestUDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public AdminService(NestUDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    // ----- Tài khoản -----
    public async Task<PagedResult<UserListDto>> GetUsersAsync(string? role, string? status, string? search, int page, int pageSize)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 5, 100);
        var q = from u in _db.Users.AsNoTracking()
                join ur in _db.UserRoles on u.Id equals ur.UserId
                join r in _db.Roles on ur.RoleId equals r.Id
                select new { u, Role = r.Name! };

        if (!string.IsNullOrEmpty(role)) q = q.Where(x => x.Role == role);
        if (status == "locked") q = q.Where(x => x.u.IsLocked);
        if (status == "active") q = q.Where(x => !x.u.IsLocked);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.u.Email!.Contains(search) || x.u.FullName.Contains(search));

        var total = await q.CountAsync();
        var rows = await q.OrderByDescending(x => x.u.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new
            {
                x.u.Id, x.u.Email, x.u.FullName, x.u.PhoneNumber, x.Role, x.u.IsLocked, x.u.CreatedAt,
                Approval = _db.LandlordProfiles.Where(p => p.UserId == x.u.Id).Select(p => (ApprovalStatus?)p.ApprovalStatus).FirstOrDefault(),
                IsStudent = _db.TenantProfiles.Where(p => p.UserId == x.u.Id).Select(p => (bool?)p.IsStudent).FirstOrDefault()
            }).ToListAsync();

        return new PagedResult<UserListDto>
        {
            Page = page, PageSize = pageSize, Total = total,
            Items = rows.Select(x => new UserListDto
            {
                Id = x.Id, Email = x.Email ?? "", FullName = x.FullName, Phone = x.PhoneNumber, Role = x.Role, IsLocked = x.IsLocked,
                LandlordApprovalStatus = x.Approval?.ToString(), IsStudent = x.IsStudent, CreatedAt = x.CreatedAt
            }).ToList()
        };
    }

    public async Task SetLockAsync(string adminId, string userId, bool locked)
    {
        if (adminId == userId) throw ApiException.BadRequest("Không thể tự khóa tài khoản của mình.");
        var u = await _users.FindByIdAsync(userId) ?? throw ApiException.NotFound("Không tìm thấy tài khoản.");
        if (await _users.IsInRoleAsync(u, Roles.Admin)) throw ApiException.Forbidden("Không thể khóa tài khoản Admin.");
        u.IsLocked = locked;
        await _users.UpdateAsync(u);
    }

    public async Task SetRoleAsync(string userId, SetRoleDto dto)
    {
        var u = await _users.FindByIdAsync(userId) ?? throw ApiException.NotFound("Không tìm thấy tài khoản.");
        var current = await _users.GetRolesAsync(u);
        if (current.Contains(Roles.Admin)) throw ApiException.Forbidden("Không đổi được role của Admin.");
        if (current.Contains(dto.Role)) return;

        if (current.Contains(Roles.Landlord) && await _db.BoardingHouses.AnyAsync(h => h.LandlordId == userId))
            throw ApiException.Conflict("Chủ trọ đang sở hữu nhà trọ, không đổi role được.");
        if (current.Contains(Roles.Tenant))
        {
            if (await _db.Contracts.AnyAsync(c => c.TenantId == userId && c.Status == ContractStatus.Active))
                throw ApiException.Conflict("Người thuê đang có hợp đồng hiệu lực, không đổi role được.");
            if (await _db.RentalRequests.AnyAsync(r => r.TenantId == userId && r.Status == RentalRequestStatus.Pending))
                throw new BusinessRuleException("Người thuê đang có yêu cầu thuê phòng chờ duyệt, không thể đổi role.");
            if (await _db.ViewingAppointments.AnyAsync(a => a.TenantId == userId && a.Status == AppointmentStatus.Pending))
                throw new BusinessRuleException("Người thuê đang có lịch xem phòng chờ xác nhận, không thể đổi role.");
        }

        await using var tx = await _db.Database.BeginTransactionAsync();
        await _users.RemoveFromRolesAsync(u, current);
        await _users.AddToRoleAsync(u, dto.Role);
        if (dto.Role == Roles.Landlord && !await _db.LandlordProfiles.AnyAsync(p => p.UserId == userId))
        {
            var tp = await _db.TenantProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
            _db.LandlordProfiles.Add(new LandlordProfile { UserId = userId, FullName = u.FullName, CitizenId = tp?.CitizenId ?? "000000000000", Phone = tp?.Phone ?? (u.PhoneNumber ?? "0000000000") });
        }
        if (dto.Role == Roles.Tenant && !await _db.TenantProfiles.AnyAsync(p => p.UserId == userId))
        {
            var lp = await _db.LandlordProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
            _db.TenantProfiles.Add(new TenantProfile { UserId = userId, FullName = u.FullName, CitizenId = lp?.CitizenId ?? "000000000000", Phone = lp?.Phone ?? (u.PhoneNumber ?? "0000000000") });
        }
        await _db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    // ----- Duyệt chủ trọ -----
    public async Task<List<LandlordApprovalDto>> GetLandlordApprovalsAsync(string? status)
    {
        var q = _db.LandlordProfiles.AsNoTracking().Include(p => p.User).AsQueryable();
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<ApprovalStatus>(status, true, out var st)) q = q.Where(p => p.ApprovalStatus == st);
        var rows = await q.OrderBy(p => p.ApprovalStatus).ThenByDescending(p => p.CreatedAt).ToListAsync();
        return rows.Select(p => new LandlordApprovalDto
        {
            Id = p.Id, UserId = p.UserId, Email = p.User.Email ?? "", FullName = p.FullName, CitizenId = p.CitizenId, Phone = p.Phone,
            ApprovalStatus = p.ApprovalStatus.ToString(), ReviewNote = p.ReviewNote, CreatedAt = p.CreatedAt, ReviewedAt = p.ReviewedAt
        }).ToList();
    }

    public async Task DecideLandlordAsync(int profileId, DecisionDto dto)
    {
        var p = await _db.LandlordProfiles.FirstOrDefaultAsync(x => x.Id == profileId) ?? throw ApiException.NotFound("Không tìm thấy hồ sơ.");
        if (p.ApprovalStatus == ApprovalStatus.Approved && dto.Approve) throw ApiException.Conflict("Hồ sơ đã được duyệt trước đó.");
        if (p.ApprovalStatus == ApprovalStatus.Rejected && !dto.Approve) throw ApiException.Conflict("Hồ sơ đã bị từ chối trước đó.");
        if (!dto.Approve && string.IsNullOrWhiteSpace(dto.Note)) throw ApiException.BadRequest("Vui lòng ghi lý do từ chối.");

        if (p.ApprovalStatus == ApprovalStatus.Approved && !dto.Approve && await _db.BoardingHouses.AnyAsync(h => h.LandlordId == p.UserId && h.Status == HouseStatus.Active))
            throw ApiException.Conflict("Chủ trọ đang có nhà trọ hoạt động, không thể chuyển sang trạng thái Từ chối. Hãy khóa tài khoản hoặc xử lý nhà trọ trước.");

        p.ApprovalStatus = dto.Approve ? ApprovalStatus.Approved : ApprovalStatus.Rejected;
        p.ReviewNote = dto.Note; p.ReviewedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    // ----- Duyệt PCCC -----
    public async Task<List<CertificateDto>> GetCertificatesAsync(string? status)
    {
        var q = _db.FireSafetyCertificates.AsNoTracking().AsQueryable();
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<CertificateStatus>(status, true, out var st)) q = q.Where(c => c.Status == st);
        return await HouseService.ProjectCertAsync(q.OrderBy(c => c.Status).ThenByDescending(c => c.UploadedAt));
    }

    public async Task<CertificateDto> GetCertificateAsync(int id)
        => (await HouseService.ProjectCertAsync(_db.FireSafetyCertificates.AsNoTracking().Where(c => c.Id == id))).FirstOrDefault()
           ?? throw ApiException.NotFound("Không tìm thấy giấy chứng nhận.");

    public async Task ReviewCertificateAsync(int id, ReviewCertificateDto dto)
    {
        var c = await _db.FireSafetyCertificates.Include(x => x.BoardingHouse).FirstOrDefaultAsync(x => x.Id == id) ?? throw ApiException.NotFound("Không tìm thấy giấy chứng nhận.");
        if (c.Status != CertificateStatus.Pending) throw ApiException.Conflict("Giấy này đã được xử lý.");
        if (!dto.Approve && string.IsNullOrWhiteSpace(dto.Note)) throw ApiException.BadRequest("Vui lòng ghi lý do từ chối.");

        await using var tx = await _db.Database.BeginTransactionAsync();
        c.Status = dto.Approve ? CertificateStatus.Approved : CertificateStatus.Rejected;
        c.ReviewNote = dto.Note; c.ReviewedAt = DateTime.UtcNow;
        if (dto.Approve)
        {
            // Nhà trọ chỉ giữ một giấy Approved (filtered unique index)
            var olds = await _db.FireSafetyCertificates.Where(x => x.BoardingHouseId == c.BoardingHouseId && x.Id != id && x.Status == CertificateStatus.Approved).ToListAsync();
            foreach (var o in olds) o.Status = CertificateStatus.Rejected;
            c.BoardingHouse.Status = HouseStatus.Active;   // Quy định 1
        }
        else if (c.BoardingHouse.Status == HouseStatus.Pending)
        {
            c.BoardingHouse.Status = HouseStatus.Rejected;
        }
        await _db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    // ----- Khiếu nại -----
    public async Task<List<ReportDto>> GetReportsAsync(string? status)
    {
        var q = _db.Reports.AsNoTracking().Include(r => r.Reporter).AsQueryable();
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<ReportStatus>(status, true, out var st)) q = q.Where(r => r.Status == st);
        var rows = await q.OrderBy(r => r.Status).ThenByDescending(r => r.CreatedAt).ToListAsync();

        var roomIds = rows.Where(r => r.TargetType == ReportTargetType.Room).Select(r => r.TargetId).ToList();
        var houseIds = rows.Where(r => r.TargetType == ReportTargetType.House).Select(r => r.TargetId).ToList();
        var landlordIds = rows.Where(r => r.TargetType == ReportTargetType.Landlord).Select(r => r.TargetId).ToList();
        var rooms = await _db.Rooms.AsNoTracking().Where(x => roomIds.Contains(x.Id)).Select(x => new { x.Id, Name = x.BoardingHouse.Name + " - P." + x.RoomNumber }).ToDictionaryAsync(x => x.Id, x => x.Name);
        var houses = await _db.BoardingHouses.AsNoTracking().Where(x => houseIds.Contains(x.Id)).Select(x => new { x.Id, x.Name }).ToDictionaryAsync(x => x.Id, x => x.Name);
        var landlords = await _db.LandlordProfiles.AsNoTracking().Where(x => landlordIds.Contains(x.Id)).Select(x => new { x.Id, x.FullName }).ToDictionaryAsync(x => x.Id, x => x.FullName);

        return rows.Select(r => new ReportDto
        {
            Id = r.Id, ReporterId = r.ReporterId, ReporterName = r.Reporter.FullName, TargetType = r.TargetType.ToString(), TargetId = r.TargetId,
            TargetName = r.TargetType switch
            {
                ReportTargetType.Room => rooms.GetValueOrDefault(r.TargetId, "(phòng đã xóa)"),
                ReportTargetType.House => houses.GetValueOrDefault(r.TargetId, "(nhà trọ đã xóa)"),
                _ => landlords.GetValueOrDefault(r.TargetId, "(chủ trọ)")
            },
            Reason = r.Reason, Status = r.Status.ToString(), ResolveNote = r.ResolveNote, CreatedAt = r.CreatedAt, ResolvedAt = r.ResolvedAt
        }).ToList();
    }

    public async Task ResolveReportAsync(int id, ResolveReportDto dto)
    {
        var r = await _db.Reports.FirstOrDefaultAsync(x => x.Id == id) ?? throw ApiException.NotFound("Không tìm thấy khiếu nại.");
        if (r.Status != ReportStatus.Open) throw ApiException.Conflict("Khiếu nại đã được xử lý.");

        await using var tx = await _db.Database.BeginTransactionAsync();
        if (!Enum.TryParse<ReportStatus>(dto.Status, true, out var rStatus))
            throw ApiException.BadRequest("Trạng thái xử lý không hợp lệ (Resolved, Dismissed).");
        r.Status = rStatus; r.ResolveNote = dto.Note; r.ResolvedAt = DateTime.UtcNow;

        if (r.Status == ReportStatus.Resolved && dto.Action == "HideHouse")
        {
            int? houseId = r.TargetType switch
            {
                ReportTargetType.House => r.TargetId,
                ReportTargetType.Room => await _db.Rooms.Where(x => x.Id == r.TargetId).Select(x => (int?)x.BoardingHouseId).FirstOrDefaultAsync(),
                _ => throw ApiException.BadRequest("Khiếu nại về chủ trọ không gắn với một tin cụ thể, hãy chọn khóa chủ trọ.")
            };
            var house = await _db.BoardingHouses.FirstOrDefaultAsync(h => h.Id == houseId);
            if (house is not null) house.IsHidden = true;
        }
        if (r.Status == ReportStatus.Resolved && dto.Action == "LockLandlord")
        {
            string? landlordUserId = r.TargetType switch
            {
                ReportTargetType.Landlord => await _db.LandlordProfiles.Where(p => p.Id == r.TargetId).Select(p => p.UserId).FirstOrDefaultAsync(),
                ReportTargetType.House => await _db.BoardingHouses.Where(h => h.Id == r.TargetId).Select(h => h.LandlordId).FirstOrDefaultAsync(),
                _ => await _db.Rooms.Where(x => x.Id == r.TargetId).Select(x => x.BoardingHouse.LandlordId).FirstOrDefaultAsync()
            };
            if (landlordUserId is not null)
            {
                var u = await _users.FindByIdAsync(landlordUserId);
                if (u is not null)
                {
                    if (await _users.IsInRoleAsync(u, Roles.Admin))
                        throw new BusinessRuleException("Không thể khóa tài khoản có vai trò Quản trị viên (Admin).");
                    u.IsLocked = true;
                    await _users.UpdateAsync(u);
                }
            }
        }
        await _db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    // ----- Nhà trọ (ẩn/hiện tin) -----
    public async Task<List<AdminHouseDto>> GetHousesAsync()
    {
        var rows = await _db.BoardingHouses.AsNoTracking().OrderBy(h => h.Name)
            .Select(h => new { h.Id, h.Name, h.Address, LandlordName = h.Landlord.FullName, h.Status, h.IsHidden, RoomCount = h.Rooms.Count })
            .ToListAsync();
        return rows.Select(h => new AdminHouseDto
        {
            Id = h.Id, Name = h.Name, Address = h.Address, LandlordName = h.LandlordName,
            Status = h.Status.ToString(), IsHidden = h.IsHidden, RoomCount = h.RoomCount
        }).ToList();
    }

    public async Task SetHouseHiddenAsync(int id, SetHouseHiddenDto dto)
    {
        var house = await _db.BoardingHouses.FirstOrDefaultAsync(h => h.Id == id) ?? throw ApiException.NotFound("Không tìm thấy nhà trọ.");
        house.IsHidden = dto.IsHidden;
        await _db.SaveChangesAsync();
    }

    // ----- Danh mục -----
    public async Task<List<CatalogItemDto>> GetCatalogAsync(string type) => type switch
    {
        "areas" => await _db.Areas.AsNoTracking().OrderBy(a => a.Name).Select(a => new CatalogItemDto { Id = a.Id, Name = a.Name }).ToListAsync(),
        "amenities" => await _db.Amenities.AsNoTracking().OrderBy(a => a.Name).Select(a => new CatalogItemDto { Id = a.Id, Name = a.Name, Icon = a.Icon }).ToListAsync(),
        "asset-types" => await _db.AssetTypes.AsNoTracking().OrderBy(a => a.Name).Select(a => new CatalogItemDto { Id = a.Id, Name = a.Name }).ToListAsync(),
        "universities" => await _db.Universities.AsNoTracking().OrderBy(a => a.Name).Select(a => new CatalogItemDto { Id = a.Id, Name = a.Name, ShortName = a.ShortName, Latitude = a.Latitude, Longitude = a.Longitude }).ToListAsync(),
        _ => throw ApiException.NotFound("Danh mục không tồn tại.")
    };

    public async Task<CatalogItemDto> CreateCatalogAsync(string type, CatalogItemDto dto)
    {
        var name = dto.Name.Trim();
        switch (type)
        {
            case "areas":
                if (await _db.Areas.AnyAsync(a => a.Name == name)) throw ApiException.Conflict("Khu vực đã tồn tại.");
                var a = new Area { Name = name }; _db.Areas.Add(a); await _db.SaveChangesAsync(); dto.Id = a.Id; break;
            case "amenities":
                if (await _db.Amenities.AnyAsync(x => x.Name == name)) throw ApiException.Conflict("Tiện ích đã tồn tại.");
                var am = new Amenity { Name = name, Icon = dto.Icon }; _db.Amenities.Add(am); await _db.SaveChangesAsync(); dto.Id = am.Id; break;
            case "asset-types":
                if (await _db.AssetTypes.AnyAsync(x => x.Name == name)) throw ApiException.Conflict("Loại tài sản đã tồn tại.");
                var at = new AssetType { Name = name }; _db.AssetTypes.Add(at); await _db.SaveChangesAsync(); dto.Id = at.Id; break;
            case "universities":
                if (await _db.Universities.AnyAsync(x => x.Name == name)) throw ApiException.Conflict("Trường đại học đã tồn tại.");
                var u = new University { Name = name, ShortName = dto.ShortName, Latitude = dto.Latitude ?? 0, Longitude = dto.Longitude ?? 0 };
                _db.Universities.Add(u); await _db.SaveChangesAsync(); dto.Id = u.Id; break;
            default: throw ApiException.NotFound("Danh mục không tồn tại.");
        }
        dto.Name = name;
        return dto;
    }

    public async Task UpdateCatalogAsync(string type, int id, CatalogItemDto dto)
    {
        var name = dto.Name.Trim();
        switch (type)
        {
            case "areas":
            {
                if (await _db.Areas.AnyAsync(a => a.Name == name && a.Id != id)) throw ApiException.Conflict("Tên khu vực đã tồn tại.");
                var e = await _db.Areas.FindAsync(id) ?? throw ApiException.NotFound();
                e.Name = name;
                break;
            }
            case "amenities":
            {
                if (await _db.Amenities.AnyAsync(a => a.Name == name && a.Id != id)) throw ApiException.Conflict("Tên tiện ích đã tồn tại.");
                var e = await _db.Amenities.FindAsync(id) ?? throw ApiException.NotFound();
                e.Name = name; e.Icon = dto.Icon;
                break;
            }
            case "asset-types":
            {
                if (await _db.AssetTypes.AnyAsync(a => a.Name == name && a.Id != id)) throw ApiException.Conflict("Tên loại tài sản đã tồn tại.");
                var e = await _db.AssetTypes.FindAsync(id) ?? throw ApiException.NotFound();
                e.Name = name;
                break;
            }
            case "universities":
            {
                if (await _db.Universities.AnyAsync(a => a.Name == name && a.Id != id)) throw ApiException.Conflict("Tên trường đại học đã tồn tại.");
                var e = await _db.Universities.FindAsync(id) ?? throw ApiException.NotFound();
                e.Name = name; e.ShortName = dto.ShortName; e.Latitude = dto.Latitude ?? e.Latitude; e.Longitude = dto.Longitude ?? e.Longitude;
                break;
            }
            default: throw ApiException.NotFound("Danh mục không tồn tại.");
        }
        await _db.SaveChangesAsync();
    }

    public async Task DeleteCatalogAsync(string type, int id)
    {
        switch (type)
        {
            case "areas":
                if (await _db.BoardingHouses.AnyAsync(h => h.AreaId == id)) throw ApiException.Conflict("Khu vực đang có nhà trọ, không thể xóa.");
                _db.Areas.Remove(await _db.Areas.FindAsync(id) ?? throw ApiException.NotFound()); break;
            case "amenities":
                if (await _db.RoomAmenities.AnyAsync(x => x.AmenityId == id)) throw ApiException.Conflict("Tiện ích đang được phòng sử dụng.");
                _db.Amenities.Remove(await _db.Amenities.FindAsync(id) ?? throw ApiException.NotFound()); break;
            case "asset-types":
                if (await _db.RoomAssets.AnyAsync(x => x.AssetTypeId == id)) throw ApiException.Conflict("Loại tài sản đang được sử dụng.");
                _db.AssetTypes.Remove(await _db.AssetTypes.FindAsync(id) ?? throw ApiException.NotFound()); break;
            case "universities":
                if (await _db.TenantProfiles.AnyAsync(x => x.UniversityId == id)) throw ApiException.Conflict("Trường đang có sinh viên đăng ký.");
                _db.Universities.Remove(await _db.Universities.FindAsync(id) ?? throw ApiException.NotFound()); break;
            default: throw ApiException.NotFound("Danh mục không tồn tại.");
        }
        await _db.SaveChangesAsync();
    }

    public async Task<AdminStatsDto> GetStatsAsync() => new()
    {
        TotalUsers = await _db.Users.CountAsync(),
        PendingLandlords = await _db.LandlordProfiles.CountAsync(p => p.ApprovalStatus == ApprovalStatus.Pending),
        PendingCertificates = await _db.FireSafetyCertificates.CountAsync(c => c.Status == CertificateStatus.Pending),
        ActiveHouses = await _db.BoardingHouses.CountAsync(h => h.Status == HouseStatus.Active),
        TotalRooms = await _db.Rooms.CountAsync(),
        ActiveContracts = await _db.Contracts.CountAsync(c => c.Status == ContractStatus.Active),
        OpenReports = await _db.Reports.CountAsync(r => r.Status == ReportStatus.Open)
    };
}
