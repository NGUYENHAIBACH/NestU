using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NestU.API.DTOs.Houses;
using NestU.API.DTOs.Rooms;
using NestU.API.Helpers;
using NestU.API.Services.Interfaces;

namespace NestU.API.Controllers.Landlord;

[ApiController]
[Route("api/landlord/houses")]
[Authorize(Policy = Policies.ApprovedLandlord)]
public class LandlordHousesController : ControllerBase
{
    private readonly IHouseService _houses;
    private readonly IRoomService _rooms;

    public LandlordHousesController(IHouseService houses, IRoomService rooms)
    {
        _houses = houses;
        _rooms = rooms;
    }

    /// <summary>Danh sách nhà trọ của tôi.</summary>
    [HttpGet]
    public async Task<ActionResult<List<HouseDto>>> GetAll() => Ok(await _houses.GetMineAsync(User.GetUserId()));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<HouseDto>> Get(int id) => Ok(await _houses.GetMineByIdAsync(User.GetUserId(), id));

    /// <summary>Tạo nhà trọ (trạng thái Pending, chờ duyệt PCCC). Giá điện ≤ 4.000.</summary>
    [HttpPost]
    public async Task<ActionResult<HouseDto>> Create(CreateHouseDto dto)
    {
        var house = await _houses.CreateAsync(User.GetUserId(), dto);
        return CreatedAtAction(nameof(Get), new { id = house.Id }, house);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateHouseDto dto)
    {
        await _houses.UpdateAsync(User.GetUserId(), id, dto);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _houses.DeleteAsync(User.GetUserId(), id);
        return NoContent();
    }

    /// <summary>Tóm tắt theo tầng: số phòng, còn trống, đang thuê, đang sửa.</summary>
    [HttpGet("{id:int}/floors")]
    public async Task<ActionResult<List<FloorSummaryDto>>> Floors(int id) => Ok(await _houses.GetFloorsAsync(User.GetUserId(), id));

    /// <summary>Tạo hàng loạt phòng cho một tầng (số phòng = tầng×100 + n, tối đa 30).</summary>
    [HttpPost("{id:int}/rooms/bulk")]
    public async Task<ActionResult<List<RoomListDto>>> BulkRooms(int id, BulkCreateRoomsDto dto)
        => StatusCode(StatusCodes.Status201Created, await _rooms.BulkCreateAsync(User.GetUserId(), id, dto));

    /// <summary>Giấy PCCC của nhà trọ.</summary>
    [HttpGet("{id:int}/certificates")]
    public async Task<ActionResult<List<CertificateDto>>> Certificates(int id) => Ok(await _houses.GetCertificatesAsync(User.GetUserId(), id));

    /// <summary>Upload giấy PCCC (multipart: certificateNo, file).</summary>
    [HttpPost("{id:int}/certificates")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<CertificateDto>> UploadCertificate(int id, [FromForm] CreateCertificateDto dto)
        => StatusCode(StatusCodes.Status201Created, await _houses.UploadCertificateAsync(User.GetUserId(), id, dto));
}
