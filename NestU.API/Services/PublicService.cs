using Microsoft.EntityFrameworkCore;
using NestU.API.Controllers.OData;
using NestU.API.Data;
using NestU.API.DTOs.Houses;
using NestU.API.DTOs.Public;
using NestU.API.DTOs.Rooms;
using NestU.API.Entities.Enums;
using NestU.API.Helpers;
using NestU.API.Services.Interfaces;

namespace NestU.API.Services;

public class PublicService : IPublicService
{
    private readonly NestUDbContext _db;
    public PublicService(NestUDbContext db) => _db = db;

    public async Task<LookupsDto> GetLookupsAsync()
    {
        return new LookupsDto
        {
            Areas = await _db.Areas.AsNoTracking().OrderBy(a => a.Name)
                .Select(a => new LookupItemDto { Id = a.Id, Name = a.Name }).ToListAsync(),
            SearchAreas = await _db.Areas.AsNoTracking()
                .Where(a => a.BoardingHouses.Any(h => h.Status == HouseStatus.Active && !h.IsHidden)).OrderBy(a => a.Name)
                .Select(a => new LookupItemDto { Id = a.Id, Name = a.Name }).ToListAsync(),
            Amenities =await _db.Amenities.AsNoTracking().OrderBy(a => a.Name)
                .Select(a => new LookupItemDto { Id = a.Id, Name = a.Name }).ToListAsync(),
            AssetTypes = await _db.AssetTypes.AsNoTracking().OrderBy(a => a.Name)
                .Select(a => new LookupItemDto { Id = a.Id, Name = a.Name }).ToListAsync(),
            Universities = await _db.Universities.AsNoTracking().OrderBy(u => u.Name)
                .Select(u => new UniversityDto { Id = u.Id, Name = u.Name, ShortName = u.ShortName, Latitude = u.Latitude, Longitude = u.Longitude })
                .ToListAsync()
        };
    }

    public async Task<HomeStatsDto> GetStatsAsync()
    {
        return new HomeStatsDto
        {
            AvailableRooms = await _db.Rooms.CountAsync(r => r.Status == RoomStatus.Available && r.BoardingHouse.Status == HouseStatus.Active && !r.BoardingHouse.IsHidden),
            ActiveHouses = await _db.BoardingHouses.CountAsync(h => h.Status == HouseStatus.Active && !h.IsHidden),
            ActiveTenants = await _db.Contracts.CountAsync(c => c.Status == ContractStatus.Active)
        };
    }

    public async Task<List<RoomListDto>> GetFeaturedRoomsAsync(int take = 6)
    {
        return await RoomQueries.PublicRooms(_db)
            .Where(r => r.Status == "Available")
            .OrderByDescending(r => r.ViewCount)
            .Take(Math.Clamp(take, 1, 12))
            .ToListAsync();
    }

    public async Task<HouseDetailDto> GetHouseDetailAsync(int houseId)
    {
        var h = await _db.BoardingHouses.AsNoTracking().Include(x => x.Area).Include(x => x.Landlord).Include(x => x.Services)
            .FirstOrDefaultAsync(x => x.Id == houseId && x.Status == HouseStatus.Active && !x.IsHidden) 
            ?? throw ApiException.NotFound("Không tìm thấy nhà trọ.");

        var rooms = await RoomQueries.PublicRooms(_db).Where(r => r.HouseId == houseId).OrderBy(r => r.Floor).ThenBy(r => r.RoomNumber).ToListAsync();

        return new HouseDetailDto
        {
            Id = h.Id, Name = h.Name, Address = h.Address, AreaId = h.AreaId, AreaName = h.Area.Name, Latitude = h.Latitude, Longitude = h.Longitude,
            TotalFloors = h.TotalFloors, Status = h.Status.ToString(), ElectricityPrice = h.ElectricityPrice, WaterPrice = h.WaterPrice,
            WaterBillingType = h.WaterBillingType.ToString(), AllowedTenantType = h.AllowedTenantType.ToString(), Description = h.Description,
            RoomCount = rooms.Count, AvailableRoomCount = rooms.Count(r => r.Status == "Available"), CertificateStatus = "Approved", CreatedAt = h.CreatedAt,
            LandlordName = h.Landlord.FullName, LandlordPhone = h.Landlord.PhoneNumber,
            Floors = Enumerable.Range(1, h.TotalFloors).Select(f => new FloorDto { Floor = f, Rooms = rooms.Where(r => r.Floor == f).ToList() }).ToList(),
            Services = h.Services.Where(s => s.IsActive).Select(s => new ServiceDto { Id = s.Id, BoardingHouseId = s.BoardingHouseId, Name = s.Name, Price = s.Price, Unit = s.Unit.ToString(), IsActive = true }).ToList()
        };
    }

    public async Task<List<MapMarkerDto>> GetMapMarkersAsync()
    {
        return await _db.BoardingHouses.AsNoTracking().Where(h => h.Status == HouseStatus.Active && !h.IsHidden && h.Latitude != null && h.Longitude != null)
            .Select(h => new MapMarkerDto
            {
                HouseId = h.Id, Name = h.Name, Latitude = h.Latitude!.Value, Longitude = h.Longitude!.Value,
                MinPrice = h.Rooms.Where(r => r.Status == RoomStatus.Available).Select(r => (decimal?)r.Price).Min() ?? 0,
                AvailableRooms = h.Rooms.Count(r => r.Status == RoomStatus.Available)
            }).ToListAsync();
    }
}

