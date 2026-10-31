using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NestU.API.DTOs.Assets;
using NestU.API.DTOs.Rooms;
using NestU.API.Helpers;
using NestU.API.Services.Interfaces;

namespace NestU.API.Controllers.Landlord;

[ApiController]
[Route("api/landlord/rooms")]
[Authorize(Policy = Policies.ApprovedLandlord)]
public class LandlordRoomsController : ControllerBase
{
    private readonly IRoomService _rooms;
    public LandlordRoomsController(IRoomService rooms) => _rooms = rooms;

    /// <summary>Phòng của tôi, lọc theo nhà trọ và tầng.</summary>
    [HttpGet]
    public async Task<ActionResult<List<RoomListDto>>> GetAll([FromQuery] int? houseId, [FromQuery] int? floor)
        => Ok(await _rooms.GetMineAsync(User.GetUserId(), houseId, floor));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RoomDetailDto>> Get(int id) => Ok(await _rooms.GetMineByIdAsync(User.GetUserId(), id));

    [HttpPost]
    public async Task<ActionResult<RoomDetailDto>> Create(CreateRoomDto dto)
    {
        var room = await _rooms.CreateAsync(User.GetUserId(), dto);
        return CreatedAtAction(nameof(Get), new { id = room.Id }, room);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateRoomDto dto)
    {
        await _rooms.UpdateAsync(User.GetUserId(), id, dto);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _rooms.DeleteAsync(User.GetUserId(), id);
        return NoContent();
    }

    /// <summary>Đặt Available hoặc Maintenance (không đổi được khi phòng đang Occupied).</summary>
    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> SetStatus(int id, SetRoomStatusDto dto)
    {
        await _rooms.SetStatusAsync(User.GetUserId(), id, dto);
        return NoContent();
    }

    // ----- Tài sản -----
    [HttpGet("{id:int}/assets")]
    public async Task<ActionResult<List<RoomAssetDto>>> Assets(int id) => Ok(await _rooms.GetAssetsAsync(User.GetUserId(), id));

    [HttpPost("{id:int}/assets")]
    public async Task<ActionResult<RoomAssetDto>> AddAsset(int id, RoomAssetDto dto)
        => StatusCode(StatusCodes.Status201Created, await _rooms.AddAssetAsync(User.GetUserId(), id, dto));

    [HttpPut("{id:int}/assets/{assetId:int}")]
    public async Task<IActionResult> UpdateAsset(int id, int assetId, RoomAssetDto dto)
    {
        await _rooms.UpdateAssetAsync(User.GetUserId(), id, assetId, dto);
        return NoContent();
    }

    [HttpDelete("{id:int}/assets/{assetId:int}")]
    public async Task<IActionResult> DeleteAsset(int id, int assetId)
    {
        await _rooms.DeleteAssetAsync(User.GetUserId(), id, assetId);
        return NoContent();
    }

    /// <summary>Sao chép tài sản của phòng này sang các phòng khác.</summary>
    [HttpPost("{id:int}/assets/copy")]
    public async Task<IActionResult> CopyAssets(int id, CopyAssetsDto dto)
    {
        await _rooms.CopyAssetsAsync(User.GetUserId(), id, dto);
        return NoContent();
    }
}
