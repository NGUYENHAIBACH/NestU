using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NestU.API.DTOs.Stats;
using NestU.API.Helpers;
using NestU.API.Services.Interfaces;

namespace NestU.API.Controllers.Landlord;

[ApiController]
[Route("api/landlord/stats")]
[Authorize(Policy = Policies.ApprovedLandlord)]
public class LandlordStatsController : ControllerBase
{
    private readonly IStatsService _stats;
    public LandlordStatsController(IStatsService stats) => _stats = stats;
    private string Uid => User.GetUserId();

    /// <summary>Thống kê tổng hợp doanh thu 12 tháng, lấp đầy, dự phóng tháng tới.</summary>
    [HttpGet("revenue")]
    public async Task<ActionResult<LandlordStatsDto>> Revenue([FromQuery] int? year, [FromQuery] int? houseId)
        => Ok(await _stats.GetLandlordStatsAsync(Uid, year, houseId));

    /// <summary>Thống kê tỷ lệ lấp đầy theo phòng và theo tầng.</summary>
    [HttpGet("occupancy")]
    public async Task<ActionResult<HouseOccupancyDto>> Occupancy([FromQuery] int? houseId)
        => Ok(await _stats.GetLandlordOccupancyAsync(Uid, houseId));
}

