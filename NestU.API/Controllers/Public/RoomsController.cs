using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NestU.API.Controllers.OData;
using NestU.API.Data;
using NestU.API.DTOs.Houses;
using NestU.API.DTOs.Rooms;
using NestU.API.Entities.Enums;
using NestU.API.Helpers;
using NestU.API.Services.Interfaces;

namespace NestU.API.Controllers.Public;

[ApiController]
[Route("api")]
public class PublicRoomsController : ControllerBase
{
    private readonly IRoomService _rooms;
    private readonly NestUDbContext _db;

    public PublicRoomsController(IRoomService rooms, NestUDbContext db)
    {
        _rooms = rooms;
        _db = db;
    }

    /// <summary>Chi tiết phòng công khai; tăng ViewCount (mỗi người 1 lần / 30 phút).</summary>
    [HttpGet("rooms/{id:int}")]
    public async Task<ActionResult<RoomDetailDto>> Room(int id)
    {
        var viewer = User.Identity?.IsAuthenticated == true
            ? "u:" + User.GetUserId()
            : "ip:" + (HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");
        return Ok(await _rooms.GetPublicDetailAsync(id, viewer));
    }

    /// <summary>Phòng nổi bật cho trang Home (nhiều lượt xem nhất, còn trống).</summary>
    [HttpGet("rooms/featured")]
    public async Task<ActionResult<List<RoomListDto>>> Featured([FromQuery] int take = 6)
        => Ok(await RoomQueries.PublicRooms(_db).Where(r => r.Status == "Available").OrderByDescending(r => r.ViewCount).Take(Math.Clamp(take, 1, 12)).ToListAsync());

    /// <summary>Chi tiết nhà trọ Active: phòng nhóm theo tầng, dịch vụ, badge PCCC.</summary>
    [HttpGet("houses/{id:int}")]
    public async Task<ActionResult<HouseDetailDto>> House(int id)
    {
        var h = await _db.BoardingHouses.AsNoTracking().Include(x => x.Area).Include(x => x.Landlord).Include(x => x.Services)
            .FirstOrDefaultAsync(x => x.Id == id && x.Status == HouseStatus.Active) ?? throw ApiException.NotFound("Không tìm thấy nhà trọ.");

        var rooms = await RoomQueries.PublicRooms(_db).Where(r => r.HouseId == id).OrderBy(r => r.Floor).ThenBy(r => r.RoomNumber).ToListAsync();

        return Ok(new HouseDetailDto
        {
            Id = h.Id, Name = h.Name, Address = h.Address, AreaId = h.AreaId, AreaName = h.Area.Name, Latitude = h.Latitude, Longitude = h.Longitude,
            TotalFloors = h.TotalFloors, Status = h.Status.ToString(), ElectricityPrice = h.ElectricityPrice, WaterPrice = h.WaterPrice,
            WaterBillingType = h.WaterBillingType.ToString(), AllowedTenantType = h.AllowedTenantType.ToString(), Description = h.Description,
            RoomCount = rooms.Count, AvailableRoomCount = rooms.Count(r => r.Status == "Available"), CertificateStatus = "Approved", CreatedAt = h.CreatedAt,
            LandlordName = h.Landlord.FullName, LandlordPhone = h.Landlord.PhoneNumber,
            Floors = Enumerable.Range(1, h.TotalFloors).Select(f => new FloorDto { Floor = f, Rooms = rooms.Where(r => r.Floor == f).ToList() }).ToList(),
            Services = h.Services.Where(s => s.IsActive).Select(s => new ServiceDto { Id = s.Id, BoardingHouseId = s.BoardingHouseId, Name = s.Name, Price = s.Price, Unit = s.Unit.ToString(), IsActive = true }).ToList()
        });
    }

    /// <summary>Marker bản đồ (Nên có).</summary>
    [HttpGet("houses/map")]
    public async Task<ActionResult<List<MapMarkerDto>>> Map()
        => Ok(await _db.BoardingHouses.AsNoTracking().Where(h => h.Status == HouseStatus.Active && h.Latitude != null && h.Longitude != null)
            .Select(h => new MapMarkerDto
            {
                HouseId = h.Id, Name = h.Name, Latitude = h.Latitude!.Value, Longitude = h.Longitude!.Value,
                MinPrice = h.Rooms.Where(r => r.Status == RoomStatus.Available).Select(r => (decimal?)r.Price).Min() ?? 0,
                AvailableRooms = h.Rooms.Count(r => r.Status == RoomStatus.Available)
            }).ToListAsync());
}

[ApiController]
[Route("api/rooms")]
public class PublicReviewsController : ControllerBase
{
    private readonly ISupportService _support;
    public PublicReviewsController(ISupportService support) => _support = support;

    /// <summary>Đánh giá công khai của một phòng.</summary>
    [HttpGet("{id:int}/reviews")]
    public async Task<ActionResult<List<NestU.API.DTOs.Support.ReviewDto>>> Reviews(int id) => Ok(await _support.GetRoomReviewsAsync(id));
}
