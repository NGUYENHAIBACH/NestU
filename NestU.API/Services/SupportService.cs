using Microsoft.EntityFrameworkCore;
using NestU.API.Data;
using NestU.API.DTOs.Admin;
using NestU.API.DTOs.Support;
using NestU.API.Entities;
using NestU.API.Entities.Enums;
using NestU.API.Helpers;
using NestU.API.Services.Interfaces;

namespace NestU.API.Services;

public class SupportService : ISupportService
{
    private readonly NestUDbContext _db;
    private readonly IRentalService _rentals;
    private readonly IFileStorage _files;

    public SupportService(NestUDbContext db, IRentalService rentals, IFileStorage files)
    {
        _db = db;
        _rentals = rentals;
        _files = files;
    }

    // ----- Sửa chữa -----
    private IQueryable<MaintenanceRequest> Maintenance() => _db.MaintenanceRequests.AsNoTracking()
        .Include(m => m.Contract).ThenInclude(c => c.Room).ThenInclude(r => r.BoardingHouse)
        .Include(m => m.Contract).ThenInclude(c => c.Tenant).Include(m => m.RoomAsset);

    private static MaintenanceDto Map(MaintenanceRequest m) => new()
    {
        Id = m.Id, ContractId = m.ContractId, RoomNumber = m.Contract.Room.RoomNumber, HouseName = m.Contract.Room.BoardingHouse.Name,
        TenantName = m.Contract.Tenant.FullName, RoomAssetId = m.RoomAssetId, AssetName = m.RoomAsset?.Name, Title = m.Title,
        Description = m.Description, ImageUrl = m.ImageUrl, Status = m.Status.ToString(), ResolveNote = m.ResolveNote, CreatedAt = m.CreatedAt, ResolvedAt = m.ResolvedAt
    };

    public async Task<List<MaintenanceDto>> GetMaintenanceAsync(string userId, string role, string? status)
    {
        var q = role == Roles.Tenant ? Maintenance().Where(m => m.Contract.TenantId == userId) : Maintenance().Where(m => m.Contract.Room.BoardingHouse.LandlordId == userId);
        if (Enum.TryParse<MaintenanceStatus>(status, true, out var st)) q = q.Where(m => m.Status == st);
        return (await q.OrderBy(m => m.Status).ThenByDescending(m => m.CreatedAt).ToListAsync()).Select(Map).ToList();
    }

    public async Task<MaintenanceDto> CreateMaintenanceAsync(string tenantId, CreateMaintenanceDto dto)
    {
        var c = await _rentals.GetOwnedContractAsync(tenantId, Roles.Tenant, dto.ContractId);
        if (c.Status != ContractStatus.Active) throw ApiException.Conflict("Hợp đồng đã kết thúc.");
        if (dto.RoomAssetId is not null && !await _db.RoomAssets.AnyAsync(a => a.Id == dto.RoomAssetId && a.RoomId == c.RoomId))
            throw ApiException.BadRequest("Tài sản không thuộc phòng của bạn.");
        string? url = null;
        if (dto.Image is not null) url = await _files.SaveAsync(dto.Image, "maintenance", new[] { ".jpg", ".jpeg", ".png", ".webp" });

        var m = new MaintenanceRequest { ContractId = dto.ContractId, RoomAssetId = dto.RoomAssetId, Title = dto.Title.Trim(), Description = dto.Description.Trim(), ImageUrl = url };
        _db.MaintenanceRequests.Add(m);
        await _db.SaveChangesAsync();
        return Map(await Maintenance().FirstAsync(x => x.Id == m.Id));
    }

    public async Task UpdateMaintenanceStatusAsync(string landlordId, int id, UpdateMaintenanceStatusDto dto)
    {
        var m = await _db.MaintenanceRequests.Include(x => x.Contract).ThenInclude(c => c.Room).ThenInclude(r => r.BoardingHouse).Include(x => x.RoomAsset)
            .FirstOrDefaultAsync(x => x.Id == id) ?? throw ApiException.NotFound("Không tìm thấy yêu cầu sửa chữa.");
        if (m.Contract.Room.BoardingHouse.LandlordId != landlordId) throw ApiException.Forbidden();
        if (m.Status == MaintenanceStatus.Resolved) throw ApiException.Conflict("Yêu cầu đã được xử lý xong.");

        m.Status = Enum.Parse<MaintenanceStatus>(dto.Status);
        m.ResolveNote = dto.Note;
        if (m.Status == MaintenanceStatus.Resolved)
        {
            m.ResolvedAt = DateTime.UtcNow;
            if (m.RoomAsset is not null && dto.AssetCondition is not null) m.RoomAsset.Condition = Enum.Parse<AssetCondition>(dto.AssetCondition);
        }
        await _db.SaveChangesAsync();
    }

    // ----- Đánh giá -----
    public async Task<ReviewDto> CreateReviewAsync(string tenantId, CreateReviewDto dto)
    {
        var c = await _db.Contracts.AsNoTracking().Include(x => x.Room).FirstOrDefaultAsync(x => x.Id == dto.ContractId) ?? throw ApiException.NotFound("Không tìm thấy hợp đồng.");
        if (c.TenantId != tenantId) throw ApiException.Forbidden("Chỉ người từng thuê phòng mới được đánh giá.");
        if (c.StartDate.Date > DateTime.Today) throw ApiException.BadRequest("Hợp đồng chưa bắt đầu, chưa thể đánh giá.");
        if (await _db.Reviews.AnyAsync(r => r.ContractId == dto.ContractId)) throw ApiException.Conflict("Bạn đã đánh giá hợp đồng này rồi.");

        var r = new Review { ContractId = dto.ContractId, Rating = dto.Rating, Comment = dto.Comment?.Trim() };
        _db.Reviews.Add(r);
        await _db.SaveChangesAsync();
        var tenantName = await _db.Users.Where(u => u.Id == tenantId).Select(u => u.FullName).FirstAsync();
        return new ReviewDto { Id = r.Id, ContractId = r.ContractId, RoomId = c.RoomId, RoomNumber = c.Room.RoomNumber, TenantName = tenantName, Rating = r.Rating, Comment = r.Comment, CreatedAt = r.CreatedAt };
    }

    public async Task<List<ReviewDto>> GetRoomReviewsAsync(int roomId)
    {
        var rows = await _db.Reviews.AsNoTracking().Include(r => r.Contract).ThenInclude(c => c.Room).Include(r => r.Contract).ThenInclude(c => c.Tenant)
            .Where(r => r.Contract.RoomId == roomId).OrderByDescending(r => r.CreatedAt).ToListAsync();
        return rows.Select(r => new ReviewDto
        {
            Id = r.Id, ContractId = r.ContractId, RoomId = r.Contract.RoomId, RoomNumber = r.Contract.Room.RoomNumber,
            TenantName = MaskName(r.Contract.Tenant.FullName), Rating = r.Rating, Comment = r.Comment, CreatedAt = r.CreatedAt
        }).ToList();
    }

    public async Task<List<ReviewDto>> GetTenantReviewsAsync(string tenantId)
    {
        var rows = await _db.Reviews.AsNoTracking().Include(r => r.Contract).ThenInclude(c => c.Room).Include(r => r.Contract).ThenInclude(c => c.Tenant)
            .Where(r => r.Contract.TenantId == tenantId).OrderByDescending(r => r.CreatedAt).ToListAsync();
        return rows.Select(r => new ReviewDto
        {
            Id = r.Id, ContractId = r.ContractId, RoomId = r.Contract.RoomId, RoomNumber = r.Contract.Room.RoomNumber,
            TenantName = r.Contract.Tenant.FullName, Rating = r.Rating, Comment = r.Comment, CreatedAt = r.CreatedAt
        }).ToList();
    }

    private static string MaskName(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 0 ? "Ẩn danh" : string.Join(" ", parts.Select((p, i) => i == parts.Length - 1 ? p : p[0] + "."));
    }

    // ----- Khiếu nại -----
    public async Task<ReportDto> CreateReportAsync(string reporterId, CreateReportDto dto)
    {
        var type = Enum.Parse<ReportTargetType>(dto.TargetType);
        var exists = type switch
        {
            ReportTargetType.Room => await _db.Rooms.AnyAsync(r => r.Id == dto.TargetId),
            ReportTargetType.House => await _db.BoardingHouses.AnyAsync(h => h.Id == dto.TargetId),
            _ => await _db.LandlordProfiles.AnyAsync(p => p.Id == dto.TargetId)
        };
        if (!exists) throw ApiException.NotFound("Đối tượng khiếu nại không tồn tại.");
        if (await _db.Reports.AnyAsync(r => r.ReporterId == reporterId && r.TargetType == type && r.TargetId == dto.TargetId && r.Status == ReportStatus.Open))
            throw ApiException.Conflict("Bạn đã gửi khiếu nại cho đối tượng này và đang chờ xử lý.");

        var rep = new Report { ReporterId = reporterId, TargetType = type, TargetId = dto.TargetId, Reason = dto.Reason.Trim() };
        _db.Reports.Add(rep);
        await _db.SaveChangesAsync();
        return (await GetMyReportsAsync(reporterId)).First(r => r.Id == rep.Id);
    }

    public async Task<List<ReportDto>> GetMyReportsAsync(string reporterId)
    {
        var rows = await _db.Reports.AsNoTracking().Include(r => r.Reporter).Where(r => r.ReporterId == reporterId).OrderByDescending(r => r.CreatedAt).ToListAsync();
        return rows.Select(r => new ReportDto
        {
            Id = r.Id, ReporterId = r.ReporterId, ReporterName = r.Reporter.FullName, TargetType = r.TargetType.ToString(), TargetId = r.TargetId,
            TargetName = r.TargetType.ToString() + " #" + r.TargetId, Reason = r.Reason, Status = r.Status.ToString(), ResolveNote = r.ResolveNote,
            CreatedAt = r.CreatedAt, ResolvedAt = r.ResolvedAt
        }).ToList();
    }
}
