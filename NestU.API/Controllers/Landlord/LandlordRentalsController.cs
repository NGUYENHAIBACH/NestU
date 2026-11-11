using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NestU.API.DTOs.Assets;
using NestU.API.DTOs.Common;
using NestU.API.DTOs.Rentals;
using NestU.API.Helpers;
using NestU.API.Services.Interfaces;

namespace NestU.API.Controllers.Landlord;

[ApiController]
[Route("api/landlord")]
[Authorize(Policy = Policies.ApprovedLandlord)]
public class LandlordRentalsController : ControllerBase
{
    private readonly IRentalService _rentals;
    public LandlordRentalsController(IRentalService rentals) => _rentals = rentals;
    private string Uid => User.GetUserId();

    // ----- Lịch xem -----
    [HttpGet("appointments")]
    public async Task<ActionResult<List<AppointmentDto>>> Appointments([FromQuery] string? status) => Ok(await _rentals.GetLandlordAppointmentsAsync(Uid, status));

    [HttpPut("appointments/{id:int}/confirm")]
    public async Task<IActionResult> ConfirmAppointment(int id) { await _rentals.DecideAppointmentAsync(Uid, id, true, null); return NoContent(); }

    [HttpPut("appointments/{id:int}/reject")]
    public async Task<IActionResult> RejectAppointment(int id, RejectDto dto) { await _rentals.DecideAppointmentAsync(Uid, id, false, dto.Reason); return NoContent(); }

    // ----- Yêu cầu thuê -----
    [HttpGet("rental-requests")]
    public async Task<ActionResult<List<RentalRequestDto>>> RentalRequests([FromQuery] string? status) => Ok(await _rentals.GetLandlordRequestsAsync(Uid, status));

    /// <summary>Duyệt thuê: sinh hợp đồng + biên bản nhận phòng, phòng → Occupied. 409 nếu phòng đã bị thuê.</summary>
    [HttpPut("rental-requests/{id:int}/approve")]
    public async Task<ActionResult<ContractDto>> Approve(int id, ApproveRentalDto dto) => Ok(await _rentals.ApproveRequestAsync(Uid, id, dto));

    [HttpPut("rental-requests/{id:int}/reject")]
    public async Task<IActionResult> Reject(int id, RejectDto dto) { await _rentals.RejectRequestAsync(Uid, id, dto.Reason); return NoContent(); }

    // ----- Hợp đồng -----
    [HttpGet("contracts")]
    public async Task<ActionResult<List<ContractDto>>> Contracts([FromQuery] string? status, [FromQuery] int? houseId)
        => Ok(await _rentals.GetLandlordContractsAsync(Uid, status, houseId));

    [HttpGet("contracts/{id:int}")]
    public async Task<ActionResult<ContractDto>> Contract(int id) => Ok(await _rentals.GetContractAsync(Uid, Roles.Landlord, id));

    [HttpPut("contracts/{id:int}/extend")]
    public async Task<IActionResult> Extend(int id, ExtendContractDto dto) { await _rentals.ExtendContractAsync(Uid, id, dto); return NoContent(); }

    /// <summary>Xác nhận đã nhận tiền cọc cho hợp đồng.</summary>
    [HttpPut("contracts/{id:int}/confirm-deposit")]
    public async Task<IActionResult> ConfirmDeposit(int id) { await _rentals.ConfirmDepositAsync(Uid, id); return NoContent(); }

    [HttpGet("contracts/{id:int}/assets")]
    public async Task<ActionResult<List<ContractAssetDto>>> ContractAssets(int id, [FromQuery] string? stage)
        => Ok(await _rentals.GetContractAssetsAsync(Uid, Roles.Landlord, id, stage));

    /// <summary>Ghi biên bản trả phòng: tình trạng + tiền khấu trừ từng tài sản.</summary>
    [HttpPut("contracts/{id:int}/checkout-assets")]
    public async Task<IActionResult> Checkout(int id, CheckoutAssetsDto dto) { await _rentals.CheckoutAssetsAsync(Uid, id, dto); return NoContent(); }

    /// <summary>Quyết toán cọc và chấm dứt hợp đồng (phòng → Available).</summary>
    [HttpPost("contracts/{id:int}/deposit-settlement")]
    public async Task<ActionResult<DepositSettlementDto>> Settle(int id, CreateDepositSettlementDto dto)
        => StatusCode(StatusCodes.Status201Created, await _rentals.SettleDepositAsync(Uid, id, dto));

    // ----- Yêu cầu gia hạn / trả phòng -----
    [HttpGet("contract-requests")]
    public async Task<ActionResult<List<ContractChangeRequestDto>>> ContractRequests([FromQuery] string? status)
        => Ok(await _rentals.GetChangeRequestsAsync(Uid, Roles.Landlord, status));

    [HttpPut("contract-requests/{id:int}/decision")]
    public async Task<IActionResult> Decide(int id, DecisionDto dto) { await _rentals.DecideChangeRequestAsync(Uid, id, dto); return NoContent(); }
}
