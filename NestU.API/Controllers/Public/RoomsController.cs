using Microsoft.AspNetCore.Mvc;
using NestU.API.DTOs.Houses;
using NestU.API.DTOs.Rooms;
using NestU.API.Helpers;
using NestU.API.Services.Interfaces;

namespace NestU.API.Controllers.Public;

[ApiController]
[Route("api")]
[Route("api/public")]
public class PublicRoomsController : ControllerBase
{
    private readonly IRoomService _rooms;
    private readonly IPublicService _publicService;

    public PublicRoomsController(IRoomService rooms, IPublicService publicService)
    {
        _rooms = rooms;
        _publicService = publicService;
    }

    /// <summary>Chi tiết phòng công khai; tăng ViewCount (mỗi người 1 lần / 30 phút).</summary>
    [HttpGet("rooms/{id:int}")]
    public async Task<ActionResult<RoomDetailDto>> Room(int id)
    {
        var viewer = User.Identity?.IsAuthenticated == true
            ? "u:" + User.GetUserId()
            : Guid.TryParse(Request.Headers["X-Visitor-Id"].ToString(), out var visitorId)
                ? "v:" + visitorId.ToString("N")   // khách qua Web App (cookie nestu_visitor)
                : "ip:" + (HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");   // gọi thẳng API, không có header
        return Ok(await _rooms.GetPublicDetailAsync(id, viewer));
    }

    /// <summary>Phòng nổi bật cho trang Home (nhiều lượt xem nhất, còn trống).</summary>
    [HttpGet("rooms/featured")]
    public async Task<ActionResult<List<RoomListDto>>> Featured([FromQuery] int take = 6)
        => Ok(await _publicService.GetFeaturedRoomsAsync(take));

    /// <summary>Chi tiết nhà trọ Active: phòng nhóm theo tầng, dịch vụ, badge PCCC.</summary>
    [HttpGet("houses/{id:int}")]
    public async Task<ActionResult<HouseDetailDto>> House(int id)
        => Ok(await _publicService.GetHouseDetailAsync(id));

    /// <summary>Marker bản đồ (Nên có).</summary>
    [HttpGet("houses/map")]
    public async Task<ActionResult<List<MapMarkerDto>>> Map()
        => Ok(await _publicService.GetMapMarkersAsync());
}

[ApiController]
[Route("api/rooms")]
[Route("api/public/rooms")]
public class PublicReviewsController : ControllerBase
{
    private readonly ISupportService _support;
    public PublicReviewsController(ISupportService support) => _support = support;

    /// <summary>Đánh giá công khai của một phòng.</summary>
    [HttpGet("{id:int}/reviews")]
    public async Task<ActionResult<List<NestU.API.DTOs.Support.ReviewDto>>> Reviews(int id) => Ok(await _support.GetRoomReviewsAsync(id));
}
