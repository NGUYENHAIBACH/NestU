using Microsoft.EntityFrameworkCore;
using NestU.API.Data;
using NestU.API.DTOs.Houses;
using NestU.API.Entities;
using NestU.API.Entities.Enums;
using NestU.API.Helpers;
using NestU.API.Services.Interfaces;

namespace NestU.API.Services;

public class HouseService : IHouseService
{
    private readonly NestUDbContext _db;
    private readonly IFileStorage _files;

    public HouseService(NestUDbContext db, IFileStorage files)
    {
        _db = db;
        _files = files;
    }

    public async Task<LandlordProfileDto> GetProfileAsync(string landlordId)
    {
        var p = await _db.LandlordProfiles.AsNoTracking().Include(x => x.User).FirstOrDefaultAsync(x => x.UserId == landlordId)
            ?? throw ApiException.NotFound("Chưa có hồ sơ chủ trọ.");
        return new LandlordProfileDto
        {
            UserId = landlordId, Email = p.User.Email ?? "", FullName = p.FullName, CitizenId = p.CitizenId, Phone = p.Phone,
            ApprovalStatus = p.ApprovalStatus.ToString(), ReviewNote = p.ReviewNote
        };
    }

    public async Task UpdateProfileAsync(string landlordId, LandlordProfileDto dto)
    {
        var p = await _db.LandlordProfiles.Include(x => x.User).FirstOrDefaultAsync(x => x.UserId == landlordId) 
            ?? throw ApiException.NotFound("Chưa có hồ sơ chủ trọ.");
        p.FullName = dto.FullName.Trim(); p.CitizenId = dto.CitizenId; p.Phone = dto.Phone;
        p.User.FullName = p.FullName; p.User.PhoneNumber = dto.Phone;

        if (p.ApprovalStatus == ApprovalStatus.Rejected)
        {
            p.ApprovalStatus = ApprovalStatus.Pending;
            p.ReviewNote = null;
            p.ReviewedAt = null;
        }

        await _db.SaveChangesAsync();
    }

    public async Task<BoardingHouse> GetOwnedHouseAsync(string landlordId, int houseId, bool track = false)
    {
        var q = track ? _db.BoardingHouses : _db.BoardingHouses.AsNoTracking();
        var house = await q.FirstOrDefaultAsync(h => h.Id == houseId) ?? throw ApiException.NotFound("Không tìm thấy nhà trọ.");
        if (house.LandlordId != landlordId) throw ApiException.Forbidden("Nhà trọ này không thuộc quyền quản lý của bạn.");
        return house;
    }

    private static async Task<List<HouseDto>> ProjectAsync(IQueryable<BoardingHouse> q)
    {
        // Enum không translate được ToString() trong SQL → lấy enum ra rồi map trong bộ nhớ
        var rows = await q.Select(h => new
        {
            h.Id, h.Name, h.Address, h.AreaId, AreaName = h.Area.Name, h.Latitude, h.Longitude, h.TotalFloors, h.Status, h.IsHidden,
            h.ElectricityPrice, h.WaterPrice, h.WaterBillingType, h.AllowedTenantType, h.Description, h.BankCode, h.BankAccount, h.AccountName,
            RoomCount = h.Rooms.Count, AvailableRoomCount = h.Rooms.Count(r => r.Status == RoomStatus.Available),
            CertStatus = h.Certificates.OrderByDescending(c => c.UploadedAt).Select(c => (CertificateStatus?)c.Status).FirstOrDefault(),
            CertNote = h.Certificates.OrderByDescending(c => c.UploadedAt).Select(c => c.ReviewNote).FirstOrDefault(),
            h.CreatedAt
        }).ToListAsync();

        return rows.Select(h => new HouseDto
        {
            Id = h.Id, Name = h.Name, Address = h.Address, AreaId = h.AreaId, AreaName = h.AreaName,
            Latitude = h.Latitude, Longitude = h.Longitude, TotalFloors = h.TotalFloors, Status = h.Status.ToString(), IsHidden = h.IsHidden,
            ElectricityPrice = h.ElectricityPrice, WaterPrice = h.WaterPrice, WaterBillingType = h.WaterBillingType.ToString(),
            AllowedTenantType = h.AllowedTenantType.ToString(), Description = h.Description,
            BankCode = h.BankCode, BankAccount = h.BankAccount, AccountName = h.AccountName,
            RoomCount = h.RoomCount, AvailableRoomCount = h.AvailableRoomCount,
            CertificateStatus = h.CertStatus?.ToString(), CertificateNote = h.CertNote, CreatedAt = h.CreatedAt
        }).ToList();
    }

    public Task<List<HouseDto>> GetMineAsync(string landlordId) =>
        ProjectAsync(_db.BoardingHouses.AsNoTracking().Where(h => h.LandlordId == landlordId).OrderByDescending(h => h.CreatedAt));

    public async Task<HouseDto> GetMineByIdAsync(string landlordId, int id)
    {
        await GetOwnedHouseAsync(landlordId, id);
        return (await ProjectAsync(_db.BoardingHouses.AsNoTracking().Where(h => h.Id == id))).First();
    }

    public async Task<HouseDto> CreateAsync(string landlordId, CreateHouseDto dto)
    {
        var house = new BoardingHouse { LandlordId = landlordId, Status = HouseStatus.Pending };
        Apply(house, dto);
        await AssignAreaAsync(house, dto);
        _db.BoardingHouses.Add(house);
        await _db.SaveChangesAsync();
        return await GetMineByIdAsync(landlordId, house.Id);
    }

    public async Task UpdateAsync(string landlordId, int id, UpdateHouseDto dto)
    {
        var house = await GetOwnedHouseAsync(landlordId, id, track: true);

        var maxFloor = await _db.Rooms.Where(r => r.BoardingHouseId == id).Select(r => (int?)r.Floor).MaxAsync() ?? 0;
        if (dto.TotalFloors < maxFloor)
            throw ApiException.BadRequest($"Không thể giảm số tầng xuống {dto.TotalFloors} vì đang có phòng ở tầng {maxFloor}.");

        Apply(house, dto);
        await AssignAreaAsync(house, dto, keepCurrent: true);
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Gán khu vực cho nhà trọ: ưu tiên khu vực chủ trọ chọn; không chọn thì suy ra từ quận/huyện,
    /// so khớp chính xác theo tên đã chuẩn hóa; quận chưa có trong danh mục thì tạo mới cùng lần lưu nhà trọ.
    /// </summary>
    private async Task AssignAreaAsync(BoardingHouse house, CreateHouseDto dto, bool keepCurrent = false)
    {
        if (dto.AreaId is > 0)
        {
            if (!await _db.Areas.AnyAsync(a => a.Id == dto.AreaId)) throw ApiException.BadRequest("Khu vực không tồn tại.");
            house.AreaId = dto.AreaId.Value;
            return;
        }

        var name = AreaNames.Normalize(dto.DistrictName);
        if (name.Length == 0)
        {
            if (keepCurrent) return;   // sửa nhà trọ mà không đổi địa chỉ hành chính → giữ khu vực cũ
            throw ApiException.BadRequest("Vui lòng chọn Quận / Huyện trong địa chỉ hoặc chọn khu vực.");
        }

        var key = AreaNames.Key(name);
        var areas = await _db.Areas.AsNoTracking().Select(a => new { a.Id, a.Name }).ToListAsync();
        var match = areas.FirstOrDefault(a => AreaNames.Key(a.Name) == key);
        if (match is not null) house.AreaId = match.Id;
        else house.Area = new Area { Name = name };
    }

    public async Task DeleteAsync(string landlordId, int id)
    {
        var house = await GetOwnedHouseAsync(landlordId, id, track: true);
        var hasActive = await _db.Contracts.AnyAsync(c => c.Room.BoardingHouseId == id && c.Status == ContractStatus.Active);
        if (hasActive) throw ApiException.Conflict("Nhà trọ đang có hợp đồng hiệu lực, không thể xóa.");
        if (await _db.Rooms.AnyAsync(r => r.BoardingHouseId == id))
            throw ApiException.Conflict("Hãy xóa hết phòng trước khi xóa nhà trọ.");
        _db.BoardingHouses.Remove(house);
        await _db.SaveChangesAsync();
    }

    public async Task<List<FloorSummaryDto>> GetFloorsAsync(string landlordId, int houseId)
    {
        var house = await GetOwnedHouseAsync(landlordId, houseId);
        var grouped = await _db.Rooms.AsNoTracking().Where(r => r.BoardingHouseId == houseId)
            .GroupBy(r => r.Floor)
            .Select(g => new FloorSummaryDto
            {
                Floor = g.Key, TotalRooms = g.Count(),
                Available = g.Count(r => r.Status == RoomStatus.Available),
                Occupied = g.Count(r => r.Status == RoomStatus.Occupied),
                Maintenance = g.Count(r => r.Status == RoomStatus.Maintenance)
            }).ToListAsync();
        // Tầng chưa có phòng vẫn hiện để chủ trọ tạo phòng
        return Enumerable.Range(1, house.TotalFloors)
            .Select(f => grouped.FirstOrDefault(g => g.Floor == f) ?? new FloorSummaryDto { Floor = f })
            .ToList();
    }

    // ----- Giấy PCCC -----
    public async Task<List<CertificateDto>> GetCertificatesAsync(string landlordId, int houseId)
    {
        await GetOwnedHouseAsync(landlordId, houseId);
        return await ProjectCertAsync(_db.FireSafetyCertificates.AsNoTracking().Where(c => c.BoardingHouseId == houseId)
            .OrderByDescending(c => c.UploadedAt));
    }

    public async Task<CertificateDto> UploadCertificateAsync(string landlordId, int houseId, CreateCertificateDto dto)
    {
        var house = await GetOwnedHouseAsync(landlordId, houseId, track: true);
        if (house.Status == HouseStatus.Active)
            throw new BusinessRuleException("Nhà trọ đã được phê duyệt hoạt động, không thể nộp thêm giấy PCCC mới.");
        if (await _db.FireSafetyCertificates.AnyAsync(c => c.BoardingHouseId == houseId && c.Status == CertificateStatus.Pending))
            throw ApiException.Conflict("Đang có giấy PCCC chờ duyệt, vui lòng đợi kết quả.");

        var url = await _files.SaveAsync(dto.File, "certificates");
        var cert = new FireSafetyCertificate { BoardingHouseId = houseId, CertificateNo = dto.CertificateNo.Trim(), FileUrl = url };
        _db.FireSafetyCertificates.Add(cert);
        if (house.Status == HouseStatus.Rejected) house.Status = HouseStatus.Pending; // nộp lại sau khi bị từ chối
        await _db.SaveChangesAsync();
        return (await ProjectCertAsync(_db.FireSafetyCertificates.AsNoTracking().Where(c => c.Id == cert.Id))).First();
    }

    public static async Task<List<CertificateDto>> ProjectCertAsync(IQueryable<FireSafetyCertificate> q)
    {
        var rows = await q.Include(c => c.BoardingHouse).ThenInclude(h => h.Landlord).ToListAsync();
        return rows.Select(c => new CertificateDto
        {
            Id = c.Id, BoardingHouseId = c.BoardingHouseId, HouseName = c.BoardingHouse.Name,
            LandlordName = c.BoardingHouse.Landlord.FullName, CertificateNo = c.CertificateNo, FileUrl = c.FileUrl,
            Status = c.Status.ToString(), ReviewNote = c.ReviewNote, UploadedAt = c.UploadedAt, ReviewedAt = c.ReviewedAt
        }).ToList();
    }

    // ----- Dịch vụ -----
    public async Task<List<ServiceDto>> GetServicesAsync(string landlordId, int? houseId)
    {
        var q = _db.Services.AsNoTracking().Where(s => s.BoardingHouse.LandlordId == landlordId);
        if (houseId is not null) q = q.Where(s => s.BoardingHouseId == houseId);
        var rows = await q.OrderBy(s => s.BoardingHouseId).ThenBy(s => s.Name).ToListAsync();
        return rows.Select(s => new ServiceDto
        {
            Id = s.Id, BoardingHouseId = s.BoardingHouseId, Name = s.Name, Price = s.Price, Unit = s.Unit.ToString(), IsActive = s.IsActive
        }).ToList();
    }

    public async Task<ServiceDto> CreateServiceAsync(string landlordId, ServiceDto dto)
    {
        await GetOwnedHouseAsync(landlordId, dto.BoardingHouseId);
        if (dto.Price < 0) throw ApiException.BadRequest("Giá dịch vụ phải lớn hơn hoặc bằng 0.");
        if (!Enum.TryParse<ServiceUnit>(dto.Unit, true, out var sUnit)) throw ApiException.BadRequest("Đơn vị dịch vụ không hợp lệ (PerMonth, PerPerson).");
        var trimmedName = dto.Name.Trim();
        if (await _db.Services.AnyAsync(s => s.BoardingHouseId == dto.BoardingHouseId && s.Name.ToLower() == trimmedName.ToLower()))
            throw ApiException.Conflict($"Dịch vụ '{trimmedName}' đã tồn tại trong nhà trọ này.");

        var s = new Service { BoardingHouseId = dto.BoardingHouseId, Name = trimmedName, Price = dto.Price, Unit = sUnit, IsActive = dto.IsActive };
        _db.Services.Add(s);
        await _db.SaveChangesAsync();
        dto.Id = s.Id;
        return dto;
    }

    public async Task UpdateServiceAsync(string landlordId, int id, ServiceDto dto)
    {
        var s = await _db.Services.Include(x => x.BoardingHouse).FirstOrDefaultAsync(x => x.Id == id) ?? throw ApiException.NotFound("Không tìm thấy dịch vụ.");
        if (s.BoardingHouse.LandlordId != landlordId) throw ApiException.Forbidden();
        if (dto.Price < 0) throw ApiException.BadRequest("Giá dịch vụ phải lớn hơn hoặc bằng 0.");
        if (!Enum.TryParse<ServiceUnit>(dto.Unit, true, out var sUnitUpdate)) throw ApiException.BadRequest("Đơn vị dịch vụ không hợp lệ (PerMonth, PerPerson).");
        var trimmedName = dto.Name.Trim();
        if (await _db.Services.AnyAsync(x => x.BoardingHouseId == s.BoardingHouseId && x.Id != id && x.Name.ToLower() == trimmedName.ToLower()))
            throw ApiException.Conflict($"Dịch vụ '{trimmedName}' đã tồn tại trong nhà trọ này.");

        s.Name = trimmedName; s.Price = dto.Price; s.Unit = sUnitUpdate; s.IsActive = dto.IsActive;
        await _db.SaveChangesAsync();
    }

    public async Task DeleteServiceAsync(string landlordId, int id)
    {
        var s = await _db.Services.Include(x => x.BoardingHouse).FirstOrDefaultAsync(x => x.Id == id) ?? throw ApiException.NotFound("Không tìm thấy dịch vụ.");
        if (s.BoardingHouse.LandlordId != landlordId) throw ApiException.Forbidden();
        if (await _db.ContractServices.AnyAsync(cs => cs.ServiceId == id && cs.Contract.Status == ContractStatus.Active))
            throw ApiException.Conflict("Dịch vụ đang được hợp đồng hiệu lực sử dụng. Hãy chuyển sang ngừng cung cấp (IsActive = false) thay vì xóa.");

        if (await _db.ContractServices.AnyAsync(cs => cs.ServiceId == id))
        {
            s.IsActive = false;
            await _db.SaveChangesAsync();
            return;
        }

        _db.Services.Remove(s);
        await _db.SaveChangesAsync();
    }

    private static void Apply(BoardingHouse h, CreateHouseDto d)
    {
        // Quy định 2: Kiểm tra tầng nghiệp vụ Service
        if (d.ElectricityPrice <= 0 || d.ElectricityPrice > 4000)
            throw ApiException.BadRequest("Giá điện phải từ 1 đến 4.000đ/kWh.");

        if (d.WaterPrice <= 0)
            throw ApiException.BadRequest("Giá nước phải lớn hơn 0.");

        if (d.TotalFloors < 1 || d.TotalFloors > 50)
            throw ApiException.BadRequest("Số tầng phải từ 1 đến 50.");

        if (d.Latitude.HasValue && (d.Latitude < -90 || d.Latitude > 90))
            throw new BusinessRuleException("Vĩ độ (Latitude) phải nằm trong khoảng từ -90 đến 90 độ.");
        if (d.Longitude.HasValue && (d.Longitude < -180 || d.Longitude > 180))
            throw new BusinessRuleException("Kinh độ (Longitude) phải nằm trong khoảng từ -180 đến 180 độ.");

        h.Name = d.Name.Trim(); h.Address = d.Address.Trim();
        h.Latitude = d.Latitude; h.Longitude = d.Longitude; h.TotalFloors = d.TotalFloors;
        h.ElectricityPrice = d.ElectricityPrice; h.WaterPrice = d.WaterPrice;
        if (!Enum.TryParse<WaterBillingType>(d.WaterBillingType, true, out var wType)) throw ApiException.BadRequest("Hình thức tính tiền nước không hợp lệ (PerM3, PerPerson).");
        if (!Enum.TryParse<AllowedTenantType>(d.AllowedTenantType, true, out var tType)) throw ApiException.BadRequest("Đối tượng cho thuê không hợp lệ (All, StudentOnly).");
        h.WaterBillingType = wType;
        h.AllowedTenantType = tType;
        h.Description = d.Description; h.BankCode = d.BankCode; h.BankAccount = d.BankAccount; h.AccountName = d.AccountName;
    }
}
