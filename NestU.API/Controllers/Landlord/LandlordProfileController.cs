using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NestU.API.DTOs.Houses;
using NestU.API.Helpers;
using NestU.API.Services.Interfaces;

namespace NestU.API.Controllers.Landlord;

/// <summary>Hồ sơ chủ trọ: không cần policy duyệt, để Landlord chờ duyệt vẫn xem được trạng thái.</summary>
[ApiController]
[Route("api/landlord/profile")]
[Authorize(Roles = Roles.Landlord)]
public class LandlordProfileController : ControllerBase
{
    private readonly IHouseService _houses;
    public LandlordProfileController(IHouseService houses) => _houses = houses;

    [HttpGet]
    public async Task<ActionResult<LandlordProfileDto>> Get()
    {
        return Ok(await _houses.GetProfileAsync(User.GetUserId()));
    }

    [HttpPut]
    public async Task<IActionResult> Update(LandlordProfileDto dto)
    {
        await _houses.UpdateProfileAsync(User.GetUserId(), dto);
        return NoContent();
    }
}
