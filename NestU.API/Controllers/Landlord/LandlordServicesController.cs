using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NestU.API.DTOs.Houses;
using NestU.API.Helpers;
using NestU.API.Services.Interfaces;

namespace NestU.API.Controllers.Landlord;

[ApiController]
[Route("api/landlord/services")]
[Authorize(Policy = Policies.ApprovedLandlord)]
public class LandlordServicesController : ControllerBase
{
    private readonly IHouseService _houses;
    public LandlordServicesController(IHouseService houses) => _houses = houses;

    [HttpGet]
    public async Task<ActionResult<List<ServiceDto>>> GetAll([FromQuery] int? houseId) => Ok(await _houses.GetServicesAsync(User.GetUserId(), houseId));

    [HttpPost]
    public async Task<ActionResult<ServiceDto>> Create(ServiceDto dto) => StatusCode(StatusCodes.Status201Created, await _houses.CreateServiceAsync(User.GetUserId(), dto));

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, ServiceDto dto)
    {
        await _houses.UpdateServiceAsync(User.GetUserId(), id, dto);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _houses.DeleteServiceAsync(User.GetUserId(), id);
        return NoContent();
    }
}
