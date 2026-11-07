using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using NestU.API.Data;
using NestU.API.DTOs.Assets;
using NestU.API.DTOs.Houses;
using NestU.API.DTOs.Rooms;
using NestU.API.Entities;
using NestU.API.Entities.Enums;
using NestU.API.Helpers;
using NestU.API.Services.Interfaces;

namespace NestU.API.Services;

public class RoomService : IRoomService
{
    private readonly NestUDbContext _db;
    private readonly IHouseService _houses;
    private readonly IMemoryCache _cache;

    public RoomService(NestUDbContext db, IHouseService houses, IMemoryCache cache)
    {
        _db = db;
        _houses = houses;
        _cache = cache;
    }

    // ----- helpers -----
    private async Task<Room> GetOwnedRoomAsync(string landlordId, int roomId, bool track = false)
    {
        var q = track ? _db.Rooms.Include(r => r.BoardingHouse) : _db.Rooms.AsNoTracking().Include(r => r.BoardingHouse);
        var room = await q.FirstOrDefaultAsync(r => r.Id == roomId) ?? throw ApiException.NotFound("Không tìm thấy phòng.");
        if (room.BoardingHouse.LandlordId != landlordId) throw ApiException.Forbidden("Phòng này không thuộc nhà trọ của bạn.");
        return room;
    }

    private static IQueryable<RoomListDto> ProjectList(IQueryable<Room> q) => q.Select(r => new RoomListDto
    {
        Id = r.Id, RoomNumber = r.RoomNumber, Floor = r.Floor, Area = r.Area, Price = r.Price, DepositAmount = r.DepositAmount,
        Capacity = r.Capacity, Status = r.Status == RoomStatus.Available ? "Available" : r.Status == RoomStatus.Occupied ? "Occupied" : "Maintenance",
        AvailabilityLabel = r.Status == RoomStatus.Available ? "Còn phòng" : r.Status == RoomStatus.Occupied ? "Hết phòng" : "Đang sửa",
        ViewCount = r.ViewCount, ThumbnailUrl = r.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault(),
        HouseId = r.BoardingHouseId, HouseName = r.BoardingHouse.Name, Address = r.Address ?? r.BoardingHouse.Address,
        AreaId = r.BoardingHouse.AreaId, AreaName = r.BoardingHouse.Area.Name, ElectricityPrice = r.BoardingHouse.ElectricityPrice,
        StudentOnly = r.BoardingHouse.AllowedTenantType == AllowedTenantType.StudentOnly,
        FireSafetyApproved = r.BoardingHouse.Status == HouseStatus.Active, CreatedAt = r.CreatedAt
    });

    private async Task<RoomDetailDto> LoadDetailAsync(int roomId)
    {
        var r = await _db.Rooms.AsNoTracking()
            .Include(x => x.BoardingHouse).ThenInclude(h => h.Area)
            .Include(x => x.BoardingHouse).ThenInclude(h => h.Landlord)
            .Include(x => x.BoardingHouse).ThenInclude(h => h.Services)
            .Include(x => x.Images).Include(x => x.RoomAmenities).ThenInclude(a => a.Amenity)
            .Include(x => x.Assets).ThenInclude(a => a.AssetType)
            .FirstOrDefaultAsync(x => x.Id == roomId) ?? throw ApiException.NotFound("Không tìm thấy phòng.");

        var ratings = await _db.Reviews.AsNoTracking().Where(v => v.Contract.RoomId == roomId).Select(v => v.Rating).ToListAsync();
        var h = r.BoardingHouse;
        return new RoomDetailDto
        {
            Id = r.Id, RoomNumber = r.RoomNumber, Floor = r.Floor, Area = r.Area, Price = r.Price, DepositAmount = r.DepositAmount,
            Capacity = r.Capacity, Status = r.Status.ToString(),
            AvailabilityLabel = r.Status == RoomStatus.Available ? "Còn phòng" : r.Status == RoomStatus.Occupied ? "Hết phòng" : "Đang sửa",
            ViewCount = r.ViewCount, ThumbnailUrl = r.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault(),
            HouseId = h.Id, HouseName = h.Name, Address = r.Address ?? h.Address, AreaId = h.AreaId, AreaName = h.Area.Name,
            ElectricityPrice = h.ElectricityPrice, StudentOnly = h.AllowedTenantType == AllowedTenantType.StudentOnly,
            FireSafetyApproved = h.Status == HouseStatus.Active, CreatedAt = r.CreatedAt,
            Description = r.Description, WaterPrice = h.WaterPrice, WaterBillingType = h.WaterBillingType.ToString(),
            TotalFloors = h.TotalFloors, Latitude = h.Latitude, Longitude = h.Longitude, LandlordName = h.Landlord.FullName,
            ImageUrls = r.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).ToList(),
            Amenities = r.RoomAmenities.Select(a => a.Amenity.Name).ToList(),
            AmenityIds = r.RoomAmenities.Select(a => a.AmenityId).ToList(),
            Assets = r.Assets.Select(a => new RoomAssetDto
            {
                Id = a.Id, RoomId = a.RoomId, AssetTypeId = a.AssetTypeId, AssetTypeName = a.AssetType.Name, Name = a.Name,
                Quantity = a.Quantity, Condition = a.Condition.ToString(), EstimatedValue = a.EstimatedValue, Note = a.Note
            }).ToList(),
            Services = h.Services.Where(s => s.IsActive).Select(s => new ServiceDto
            {
                Id = s.Id, BoardingHouseId = s.BoardingHouseId, Name = s.Name, Price = s.Price, Unit = s.Unit.ToString(), IsActive = s.IsActive
            }).ToList(),
            AverageRating = ratings.Count == 0 ? null : Math.Round(ratings.Average(), 1),
            ReviewCount = ratings.Count
        };
    }

    // ----- Landlord -----
    public async Task<List<RoomListDto>> GetMineAsync(string landlordId, int? houseId, int? floor)
    {
        var q = _db.Rooms.AsNoTracking().Where(r => r.BoardingHouse.LandlordId == landlordId);
        if (houseId is not null) q = q.Where(r => r.BoardingHouseId == houseId);
        if (floor is not null) q = q.Where(r => r.Floor == floor);
        return await ProjectList(q.OrderBy(r => r.BoardingHouseId).ThenBy(r => r.Floor).ThenBy(r => r.RoomNumber)).ToListAsync();
    }

    public async Task<RoomDetailDto> GetMineByIdAsync(string landlordId, int roomId)
    {
        await GetOwnedRoomAsync(landlordId, roomId);
        return await LoadDetailAsync(roomId);
    }

    public async Task<RoomDetailDto> CreateAsync(string landlordId, CreateRoomDto dto)
    {
        var house = await _houses.GetOwnedHouseAsync(landlordId, dto.BoardingHouseId);
        if (dto.Floor < 1 || dto.Floor > house.TotalFloors) throw ApiException.BadRequest($"Tầng phòng phải từ 1 đến {house.TotalFloors}.");
        if (await _db.Rooms.AnyAsync(r => r.BoardingHouseId == house.Id && r.RoomNumber == dto.RoomNumber.Trim()))
            throw ApiException.Conflict($"Số phòng {dto.RoomNumber} đã tồn tại.");
        await ValidateAmenitiesAndAssets(dto.AmenityIds, dto.Assets);

        var room = new Room
        {
            BoardingHouseId = house.Id, RoomNumber = dto.RoomNumber.Trim(), Floor = dto.Floor, Area = dto.Area, Price = dto.Price,
            DepositAmount = dto.DepositAmount ?? dto.Price, Capacity = dto.Capacity, Address = dto.Address, Description = dto.Description
        };
        ApplyAmenities(room, dto.AmenityIds);
        ApplyImages(room, dto.ImageUrls);
        foreach (var a in dto.Assets) room.Assets.Add(new RoomAsset { AssetTypeId = a.AssetTypeId, Name = a.Name.Trim(), Quantity = a.Quantity, EstimatedValue = a.EstimatedValue });
        _db.Rooms.Add(room);
        await _db.SaveChangesAsync();
        return await LoadDetailAsync(room.Id);
    }

    public async Task<List<RoomListDto>> BulkCreateAsync(string landlordId, int houseId, BulkCreateRoomsDto dto)
    {
        var house = await _houses.GetOwnedHouseAsync(landlordId, houseId);
        if (dto.Floor < 1 || dto.Floor > house.TotalFloors) throw ApiException.BadRequest($"Tầng phòng phải từ 1 đến {house.TotalFloors}.");
        if (dto.FromNumber > dto.ToNumber) throw ApiException.BadRequest("Số bắt đầu phải nhỏ hơn hoặc bằng số kết thúc.");
        if (dto.ToNumber - dto.FromNumber + 1 > 30) throw ApiException.BadRequest("Mỗi lần tạo tối đa 30 phòng.");
        await ValidateAmenitiesAndAssets(dto.AmenityIds, dto.Assets);

        // Quy ước số phòng: tầng × 100 + n → 301, 302, ...
        var numbers = Enumerable.Range(dto.FromNumber, dto.ToNumber - dto.FromNumber + 1).Select(n => (dto.Floor * 100 + n).ToString()).ToList();
        var existing = await _db.Rooms.Where(r => r.BoardingHouseId == houseId && numbers.Contains(r.RoomNumber)).Select(r => r.RoomNumber).ToListAsync();
        if (existing.Count > 0) throw ApiException.Conflict($"Các số phòng đã tồn tại: {string.Join(", ", existing)}. Không tạo phòng nào.");

        await using var tx = await _db.Database.BeginTransactionAsync();
        var rooms = new List<Room>();
        foreach (var num in numbers)
        {
            var room = new Room
            {
                BoardingHouseId = houseId, RoomNumber = num, Floor = dto.Floor, Area = dto.Area, Price = dto.Price,
                DepositAmount = dto.DepositAmount ?? dto.Price, Capacity = dto.Capacity, Address = dto.Address, Description = dto.Description
            };
            ApplyAmenities(room, dto.AmenityIds);
            foreach (var a in dto.Assets) room.Assets.Add(new RoomAsset { AssetTypeId = a.AssetTypeId, Name = a.Name.Trim(), Quantity = a.Quantity, EstimatedValue = a.EstimatedValue });
            rooms.Add(room);
        }
        _db.Rooms.AddRange(rooms);
        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        var ids = rooms.Select(r => r.Id).ToList();
        return await ProjectList(_db.Rooms.AsNoTracking().Where(r => ids.Contains(r.Id)).OrderBy(r => r.RoomNumber)).ToListAsync();
    }

    public async Task UpdateAsync(string landlordId, int roomId, UpdateRoomDto dto)
    {
        var room = await _db.Rooms.Include(r => r.BoardingHouse).Include(r => r.RoomAmenities).Include(r => r.Images)
            .FirstOrDefaultAsync(r => r.Id == roomId) ?? throw ApiException.NotFound("Không tìm thấy phòng.");
        if (room.BoardingHouse.LandlordId != landlordId) throw ApiException.Forbidden();
        if (room.Status == RoomStatus.Occupied)
            throw ApiException.Conflict("Phòng đang có người thuê, không thể chỉnh sửa thông tin phòng.");
        if (dto.Floor < 1 || dto.Floor > room.BoardingHouse.TotalFloors)
            throw ApiException.BadRequest($"Tầng phòng phải từ 1 đến {room.BoardingHouse.TotalFloors}.");
        if (await _db.Rooms.AnyAsync(r => r.BoardingHouseId == room.BoardingHouseId && r.RoomNumber == dto.RoomNumber.Trim() && r.Id != roomId))
            throw ApiException.Conflict($"Số phòng {dto.RoomNumber} đã tồn tại.");
        await ValidateAmenitiesAndAssets(dto.AmenityIds, new());

        room.RoomNumber = dto.RoomNumber.Trim(); room.Floor = dto.Floor; room.Area = dto.Area; room.Price = dto.Price;
        room.DepositAmount = dto.DepositAmount; room.Capacity = dto.Capacity; room.Address = dto.Address; room.Description = dto.Description;
        room.RoomAmenities.Clear(); ApplyAmenities(room, dto.AmenityIds);
        room.Images.Clear(); ApplyImages(room, dto.ImageUrls);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(string landlordId, int roomId)
    {
        var room = await GetOwnedRoomAsync(landlordId, roomId, track: true);
        if (await _db.Contracts.AnyAsync(c => c.RoomId == roomId))
            throw ApiException.Conflict("Phòng đã có hợp đồng, không thể xóa. Hãy đặt trạng thái Đang sửa nếu không cho thuê.");
        if (await _db.RentalRequests.AnyAsync(r => r.RoomId == roomId && r.Status == RentalRequestStatus.Pending))
            throw ApiException.Conflict("Phòng đang có yêu cầu thuê chờ duyệt, không thể xóa.");
        if (await _db.ViewingAppointments.AnyAsync(a => a.RoomId == roomId && (a.Status == AppointmentStatus.Pending || (a.Status == AppointmentStatus.Confirmed && a.ScheduledAt > DateTime.Now))))
            throw ApiException.Conflict("Phòng đang có lịch xem chờ hoặc đã xác nhận, không thể xóa.");

        await using var tx = await _db.Database.BeginTransactionAsync();
        var requests = await _db.RentalRequests.Where(r => r.RoomId == roomId).ToListAsync();
        if (requests.Count > 0) _db.RentalRequests.RemoveRange(requests);

        var appointments = await _db.ViewingAppointments.Where(a => a.RoomId == roomId).ToListAsync();
        if (appointments.Count > 0) _db.ViewingAppointments.RemoveRange(appointments);

        _db.Rooms.Remove(room);
        await _db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    public async Task SetStatusAsync(string landlordId, int roomId, SetRoomStatusDto dto)
    {
        var room = await GetOwnedRoomAsync(landlordId, roomId, track: true);
        if (room.Status == RoomStatus.Occupied || await _db.Contracts.AnyAsync(c => c.RoomId == roomId && c.Status == ContractStatus.Active))
            throw ApiException.BadRequest("Phòng đang có người thuê, không đổi trạng thái thủ công được.");
        if (!Enum.TryParse<RoomStatus>(dto.Status, true, out var rStatus))
            throw ApiException.BadRequest("Trạng thái phòng không hợp lệ (Available, Maintenance).");
        if (rStatus == RoomStatus.Occupied)
            throw ApiException.BadRequest("Chỉ được chuyển trạng thái sang Còn trống (Available) hoặc Đang sửa (Maintenance).");
        if (rStatus == RoomStatus.Maintenance)
        {
            if (await _db.RentalRequests.AnyAsync(r => r.RoomId == roomId && r.Status == RentalRequestStatus.Pending))
                throw ApiException.Conflict("Phòng đang có yêu cầu thuê chờ duyệt, vui lòng xử lý yêu cầu trước khi chuyển sang Đang sửa.");
            if (await _db.ViewingAppointments.AnyAsync(a => a.RoomId == roomId && a.Status == AppointmentStatus.Pending))
                throw ApiException.Conflict("Phòng đang có lịch xem chờ xác nhận, vui lòng xử lý lịch xem trước khi chuyển sang Đang sửa.");
        }
        room.Status = rStatus;
        await _db.SaveChangesAsync();
    }

    // ----- Tài sản -----
    public async Task<List<RoomAssetDto>> GetAssetsAsync(string landlordId, int roomId)
    {
        await GetOwnedRoomAsync(landlordId, roomId);
        var list = await _db.RoomAssets.AsNoTracking().Include(a => a.AssetType).Where(a => a.RoomId == roomId).OrderBy(a => a.Name).ToListAsync();
        return list.Select(a => new RoomAssetDto
        {
            Id = a.Id, RoomId = a.RoomId, AssetTypeId = a.AssetTypeId, AssetTypeName = a.AssetType.Name, Name = a.Name,
            Quantity = a.Quantity, Condition = a.Condition.ToString(), EstimatedValue = a.EstimatedValue, Note = a.Note
        }).ToList();
    }

    public async Task<RoomAssetDto> AddAssetAsync(string landlordId, int roomId, RoomAssetDto dto)
    {
        await GetOwnedRoomAsync(landlordId, roomId);
        if (dto.Quantity < 1) throw new BusinessRuleException("Số lượng tài sản phải từ 1 trở lên.");
        if (dto.EstimatedValue < 0) throw new BusinessRuleException("Giá trị ước tính tài sản không được âm.");
        if (!await _db.AssetTypes.AnyAsync(t => t.Id == dto.AssetTypeId)) throw ApiException.BadRequest("Loại tài sản không tồn tại.");
        if (!Enum.TryParse<AssetCondition>(dto.Condition, true, out var aCond))
            throw ApiException.BadRequest("Tình trạng tài sản không hợp lệ (Good, Damaged, Broken).");
        var a = new RoomAsset
        {
            RoomId = roomId, AssetTypeId = dto.AssetTypeId, Name = dto.Name.Trim(), Quantity = dto.Quantity,
            Condition = aCond, EstimatedValue = dto.EstimatedValue, Note = dto.Note
        };
        _db.RoomAssets.Add(a);
        await _db.SaveChangesAsync();
        dto.Id = a.Id; dto.RoomId = roomId;
        dto.AssetTypeName = await _db.AssetTypes.Where(t => t.Id == a.AssetTypeId).Select(t => t.Name).FirstAsync();
        return dto;
    }

    public async Task UpdateAssetAsync(string landlordId, int roomId, int assetId, RoomAssetDto dto)
    {
        await GetOwnedRoomAsync(landlordId, roomId);
        if (dto.Quantity < 1) throw new BusinessRuleException("Số lượng tài sản phải từ 1 trở lên.");
        if (dto.EstimatedValue < 0) throw new BusinessRuleException("Giá trị ước tính tài sản không được âm.");
        var a = await _db.RoomAssets.FirstOrDefaultAsync(x => x.Id == assetId && x.RoomId == roomId) ?? throw ApiException.NotFound("Không tìm thấy tài sản.");
        if (!await _db.AssetTypes.AnyAsync(t => t.Id == dto.AssetTypeId)) throw ApiException.BadRequest("Loại tài sản không tồn tại.");
        if (!Enum.TryParse<AssetCondition>(dto.Condition, true, out var aCondUpdate))
            throw ApiException.BadRequest("Tình trạng tài sản không hợp lệ (Good, Damaged, Broken).");
        a.AssetTypeId = dto.AssetTypeId; a.Name = dto.Name.Trim(); a.Quantity = dto.Quantity;
        a.Condition = aCondUpdate; a.EstimatedValue = dto.EstimatedValue; a.Note = dto.Note;
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAssetAsync(string landlordId, int roomId, int assetId)
    {
        await GetOwnedRoomAsync(landlordId, roomId);
        var a = await _db.RoomAssets.FirstOrDefaultAsync(x => x.Id == assetId && x.RoomId == roomId) ?? throw ApiException.NotFound("Không tìm thấy tài sản.");
        if (await _db.ContractAssets.AnyAsync(c => c.RoomAssetId == assetId))
            throw ApiException.Conflict("Tài sản đã nằm trong biên bản bàn giao, không thể xóa.");
        _db.RoomAssets.Remove(a);
        await _db.SaveChangesAsync();
    }

    public async Task CopyAssetsAsync(string landlordId, int roomId, CopyAssetsDto dto)
    {
        var source = await GetOwnedRoomAsync(landlordId, roomId);
        var assets = await _db.RoomAssets.AsNoTracking().Where(a => a.RoomId == roomId).ToListAsync();
        if (assets.Count == 0) throw ApiException.BadRequest("Phòng nguồn chưa có tài sản.");

        var targets = await _db.Rooms.Include(r => r.BoardingHouse).Include(r => r.Assets)
            .Where(r => dto.TargetRoomIds.Contains(r.Id) && r.Id != roomId).ToListAsync();
        if (targets.Count != dto.TargetRoomIds.Distinct().Count(x => x != roomId)) throw ApiException.NotFound("Có phòng đích không tồn tại.");
        if (targets.Any(t => t.BoardingHouse.LandlordId != landlordId)) throw ApiException.Forbidden("Có phòng đích không thuộc nhà trọ của bạn.");
        if (targets.Any(t => t.Status == RoomStatus.Occupied)) throw ApiException.Conflict("Không thể sao chép tài sản vào phòng đang có người thuê.");

        await using var tx = await _db.Database.BeginTransactionAsync();
        foreach (var t in targets)
        {
            if (dto.ReplaceExisting)
            {
                var removable = t.Assets.Where(a => !_db.ContractAssets.Any(c => c.RoomAssetId == a.Id)).ToList();
                _db.RoomAssets.RemoveRange(removable);
            }
            foreach (var a in assets)
                t.Assets.Add(new RoomAsset { AssetTypeId = a.AssetTypeId, Name = a.Name, Quantity = a.Quantity, Condition = AssetCondition.Good, EstimatedValue = a.EstimatedValue, Note = a.Note });
        }
        await _db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    // ----- Công khai -----
    public async Task<RoomDetailDto> GetPublicDetailAsync(int roomId, string viewerKey)
    {
        var info = await _db.Rooms.AsNoTracking().Where(r => r.Id == roomId)
            .Select(r => new { r.BoardingHouse.Status }).FirstOrDefaultAsync() ?? throw ApiException.NotFound("Không tìm thấy phòng.");
        if (info.Status != HouseStatus.Active) throw ApiException.NotFound("Phòng này chưa được công khai.");

        // Lượt xem: mỗi người (IP/user) chỉ tính 1 lần trong 30 phút
        var cacheKey = $"view:{roomId}:{viewerKey}";
        if (!_cache.TryGetValue(cacheKey, out _))
        {
            _cache.Set(cacheKey, true, TimeSpan.FromMinutes(30));
            await _db.Rooms.Where(r => r.Id == roomId).ExecuteUpdateAsync(s => s.SetProperty(r => r.ViewCount, r => r.ViewCount + 1));
        }
        return await LoadDetailAsync(roomId);
    }

    public IQueryable<RoomListDto> GetPublicRoomsQuery() => NestU.API.Controllers.OData.RoomQueries.PublicRooms(_db);

    // ----- private -----
    private async Task ValidateAmenitiesAndAssets(List<int> amenityIds, List<AssetTemplateDto> assets)
    {
        if (amenityIds.Count > 0)
        {
            var ok = await _db.Amenities.CountAsync(a => amenityIds.Contains(a.Id));
            if (ok != amenityIds.Distinct().Count()) throw ApiException.BadRequest("Có tiện ích không tồn tại.");
        }
        if (assets.Count > 0)
        {
            if (assets.Any(a => a.Quantity < 1))
                throw new BusinessRuleException("Số lượng tài sản trong danh sách phải từ 1 trở lên.");
            if (assets.Any(a => a.EstimatedValue < 0))
                throw new BusinessRuleException("Giá trị ước tính tài sản không được âm.");
            var typeIds = assets.Select(a => a.AssetTypeId).Distinct().ToList();
            var ok = await _db.AssetTypes.CountAsync(t => typeIds.Contains(t.Id));
            if (ok != typeIds.Count) throw ApiException.BadRequest("Có loại tài sản không tồn tại.");
        }
    }

    private static void ApplyAmenities(Room room, List<int> ids)
    {
        foreach (var id in ids.Distinct()) room.RoomAmenities.Add(new RoomAmenity { AmenityId = id });
    }

    private static void ApplyImages(Room room, List<string> urls)
    {
        var i = 0;
        foreach (var u in urls.Where(u => !string.IsNullOrWhiteSpace(u)).Distinct().Take(20))
        {
            var trimmed = u.Trim();
            if (trimmed.Length > 500) throw ApiException.BadRequest("Đường dẫn ảnh không được vượt quá 500 ký tự.");
            room.Images.Add(new RoomImage { Url = trimmed, SortOrder = i++ });
        }
    }
}
