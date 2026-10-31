using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NestU.API.DTOs.Billing;
using NestU.API.DTOs.Common;
using NestU.API.DTOs.Support;
using NestU.API.Helpers;
using NestU.API.Services.Interfaces;

namespace NestU.API.Controllers.Landlord;

[ApiController]
[Route("api/landlord")]
[Authorize(Policy = Policies.ApprovedLandlord)]
public class LandlordBillingController : ControllerBase
{
    private readonly IBillingService _billing;
    private readonly ISupportService _support;

    public LandlordBillingController(IBillingService billing, ISupportService support)
    {
        _billing = billing;
        _support = support;
    }
    private string Uid => User.GetUserId();

    // ----- Chỉ số điện nước -----
    [HttpGet("readings")]
    public async Task<ActionResult<List<UtilityReadingDto>>> Readings([FromQuery] int? contractId, [FromQuery] int? houseId)
        => Ok(await _billing.GetReadingsAsync(Uid, contractId, houseId));

    /// <summary>Ghi chỉ số tháng; 400 nếu nhỏ hơn tháng trước, 409 nếu đã ghi.</summary>
    [HttpPost("readings")]
    public async Task<ActionResult<UtilityReadingDto>> AddReading(UtilityReadingDto dto) => StatusCode(StatusCodes.Status201Created, await _billing.AddReadingAsync(Uid, dto));

    // ----- Hóa đơn -----
    /// <summary>status = Unpaid | PendingConfirm | Paid | Overdue</summary>
    [HttpGet("invoices")]
    public async Task<ActionResult<List<InvoiceDto>>> Invoices([FromQuery] string? status, [FromQuery] int? houseId) => Ok(await _billing.GetLandlordInvoicesAsync(Uid, status, houseId));

    [HttpGet("invoices/{id:int}")]
    public async Task<ActionResult<InvoiceDto>> Invoice(int id) => Ok(await _billing.GetInvoiceAsync(Uid, Roles.Landlord, id));

    /// <summary>Lập hóa đơn tháng từ chỉ số + dịch vụ; 409 nếu trùng tháng.</summary>
    [HttpPost("invoices/generate")]
    public async Task<ActionResult<InvoiceDto>> Generate(GenerateInvoiceDto dto) => StatusCode(StatusCodes.Status201Created, await _billing.GenerateInvoiceAsync(Uid, dto));

    [HttpPut("invoices/{id:int}/confirm-payment")]
    public async Task<IActionResult> ConfirmPayment(int id) { await _billing.ConfirmPaymentAsync(Uid, id); return NoContent(); }

    [HttpPut("invoices/{id:int}/reject-payment")]
    public async Task<IActionResult> RejectPayment(int id, RejectDto dto) { await _billing.RejectPaymentAsync(Uid, id, dto.Reason); return NoContent(); }

    // ----- Sự cố -----
    [HttpGet("maintenance-requests")]
    public async Task<ActionResult<List<MaintenanceDto>>> Maintenance([FromQuery] string? status) => Ok(await _support.GetMaintenanceAsync(Uid, Roles.Landlord, status));

    [HttpPut("maintenance-requests/{id:int}/status")]
    public async Task<IActionResult> UpdateMaintenance(int id, UpdateMaintenanceStatusDto dto) { await _support.UpdateMaintenanceStatusAsync(Uid, id, dto); return NoContent(); }
}
