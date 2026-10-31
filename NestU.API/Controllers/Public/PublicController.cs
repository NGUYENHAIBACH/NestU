using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NestU.API.Data;
using NestU.API.DTOs.Public;
using NestU.API.Entities.Enums;

namespace NestU.API.Controllers.Public;

[ApiController]
[Route("api/public")]
public class PublicController : ControllerBase
{
    private readonly NestUDbContext _db;
    public PublicController(NestUDbContext db) => _db = db;

    /// <summary>Danh mục dùng cho form: khu vực, tiện ích, loại tài sản, trường (kèm tọa độ).</summary>
    [HttpGet("lookups")]
    public async Task<ActionResult<LookupsDto>> Lookups()
    {
        return Ok(new LookupsDto
        {
            Areas = await _db.Areas.AsNoTracking().OrderBy(a => a.Name)
                .Select(a => new LookupItemDto { Id = a.Id, Name = a.Name }).ToListAsync(),
            Amenities = await _db.Amenities.AsNoTracking().OrderBy(a => a.Name)
                .Select(a => new LookupItemDto { Id = a.Id, Name = a.Name }).ToListAsync(),
            AssetTypes = await _db.AssetTypes.AsNoTracking().OrderBy(a => a.Name)
                .Select(a => new LookupItemDto { Id = a.Id, Name = a.Name }).ToListAsync(),
            Universities = await _db.Universities.AsNoTracking().OrderBy(u => u.Name)
                .Select(u => new UniversityDto { Id = u.Id, Name = u.Name, ShortName = u.ShortName, Latitude = u.Latitude, Longitude = u.Longitude })
                .ToListAsync()
        });
    }

    /// <summary>Số liệu trang Home.</summary>
    [HttpGet("stats")]
    public async Task<ActionResult<HomeStatsDto>> Stats()
    {
        return Ok(new HomeStatsDto
        {
            AvailableRooms = await _db.Rooms.CountAsync(r => r.Status == RoomStatus.Available && r.BoardingHouse.Status == HouseStatus.Active),
            ActiveHouses = await _db.BoardingHouses.CountAsync(h => h.Status == HouseStatus.Active),
            ActiveTenants = await _db.Contracts.CountAsync(c => c.Status == ContractStatus.Active)
        });
    }
}
