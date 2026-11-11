using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NestU.API.DTOs.Admin;
using NestU.API.DTOs.Assets;
using NestU.API.DTOs.Billing;
using NestU.API.DTOs.Rentals;
using NestU.API.DTOs.Support;
using NestU.API.Helpers;
using NestU.API.Services.Interfaces;

namespace NestU.API.Controllers.Tenant;

[ApiController]
[Route("api/tenant")]
[Authorize(Roles = Roles.Tenant)]
public class TenantController : ControllerBase
{
    private readonly IRentalService _rentals;
    private readonly IBillingService _billing;
    private readonly ISupportService _support;

    public TenantController(IRentalService rentals, IBillingService billing, ISupportService support)
    {
        _rentals = rentals;
        _billing = billing;
        _support = support;
    }

    private string Uid => User.GetUserId();

    // ----- Hồ sơ -----
    [HttpGet("profile")]
    public async Task<ActionResult<TenantProfileDto>> Profile() => Ok(await _rentals.GetTenantProfileAsync(Uid));

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile(TenantProfileDto dto) { await _rentals.UpdateTenantProfileAsync(Uid, dto); return NoContent(); }

    // ----- Lịch xem -----
    [HttpGet("appointments")]
    public async Task<ActionResult<List<AppointmentDto>>> Appointments() => Ok(await _rentals.GetTenantAppointmentsAsync(Uid));

    [HttpPost("appointments")]
    public async Task<ActionResult<AppointmentDto>> CreateAppointment(CreateAppointmentDto dto)
        => StatusCode(StatusCodes.Status201Created, await _rentals.CreateAppointmentAsync(Uid, dto));

    [HttpPut("appointments/{id:int}/cancel")]
    public async Task<IActionResult> CancelAppointment(int id) { await _rentals.CancelAppointmentAsync(Uid, id); return NoContent(); }

    // ----- Yêu cầu thuê -----
    [HttpGet("rental-requests")]
    public async Task<ActionResult<List<RentalRequestDto>>> RentalRequests() => Ok(await _rentals.GetTenantRequestsAsync(Uid));

    /// <summary>Gửi yêu cầu thuê. 400 nếu phòng không Available hoặc nhà trọ chưa duyệt PCCC.</summary>
    [HttpPost("rental-requests")]
    public async Task<ActionResult<RentalRequestDto>> CreateRentalRequest(CreateRentalRequestDto dto)
        => StatusCode(StatusCodes.Status201Created, await _rentals.CreateRequestAsync(Uid, dto));

    [HttpPut("rental-requests/{id:int}/cancel")]
    public async Task<IActionResult> CancelRentalRequest(int id) { await _rentals.CancelRequestAsync(Uid, id); return NoContent(); }

    // ----- Hợp đồng -----
    [HttpGet("contracts")]
    public async Task<ActionResult<List<ContractDto>>> Contracts() => Ok(await _rentals.GetTenantContractsAsync(Uid));

    [HttpGet("contracts/{id:int}")]
    public async Task<ActionResult<ContractDto>> Contract(int id) => Ok(await _rentals.GetContractAsync(Uid, Roles.Tenant, id));

    /// <summary>Biên bản bàn giao; stage=CheckIn|CheckOut.</summary>
    [HttpGet("contracts/{id:int}/assets")]
    public async Task<ActionResult<List<ContractAssetDto>>> ContractAssets(int id, [FromQuery] string? stage)
        => Ok(await _rentals.GetContractAssetsAsync(Uid, Roles.Tenant, id, stage));

    /// <summary>Xác nhận biên bản nhận phòng (sau đó bị khóa).</summary>
    [HttpPut("contracts/{id:int}/assets/confirm")]
    public async Task<IActionResult> ConfirmAssets(int id) { await _rentals.ConfirmCheckInAsync(Uid, id); return NoContent(); }

    [HttpPost("contracts/{id:int}/renewal-requests")]
    public async Task<ActionResult<ContractChangeRequestDto>> Renew(int id, CreateContractChangeDto dto)
        => StatusCode(StatusCodes.Status201Created, await _rentals.CreateChangeRequestAsync(Uid, id, "Renew", dto));

    [HttpPost("contracts/{id:int}/termination-requests")]
    public async Task<ActionResult<ContractChangeRequestDto>> Terminate(int id, CreateContractChangeDto dto)
        => StatusCode(StatusCodes.Status201Created, await _rentals.CreateChangeRequestAsync(Uid, id, "Terminate", dto));

    [HttpGet("contract-requests")]
    public async Task<ActionResult<List<ContractChangeRequestDto>>> ContractRequests([FromQuery] string? status)
        => Ok(await _rentals.GetChangeRequestsAsync(Uid, Roles.Tenant, status));

    /// <summary>Mã QR chuyển khoản đặt cọc hợp đồng.</summary>
    [HttpGet("contracts/{id:int}/deposit-qr")]
    public async Task<ActionResult<QrDto>> DepositQr(int id) => Ok(await _rentals.GetDepositQrAsync(Uid, id));

    // ----- Hóa đơn -----
    [HttpGet("invoices")]
    public async Task<ActionResult<List<InvoiceDto>>> Invoices() => Ok(await _billing.GetTenantInvoicesAsync(Uid));

    [HttpGet("invoices/{id:int}")]
    public async Task<ActionResult<InvoiceDto>> Invoice(int id) => Ok(await _billing.GetInvoiceAsync(Uid, Roles.Tenant, id));

    /// <summary>Báo đã thanh toán, kèm ảnh chuyển khoản (multipart).</summary>
    [HttpPost("invoices/{id:int}/payments")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ReportPayment(int id, [FromForm] ReportPaymentDto dto) { await _billing.ReportPaymentAsync(Uid, id, dto); return NoContent(); }

    /// <summary>Mã VietQR cho hóa đơn (Nên có).</summary>
    [HttpGet("invoices/{id:int}/qr")]
    public async Task<ActionResult<QrDto>> Qr(int id) => Ok(await _billing.GetQrAsync(Uid, id));

    // ----- Sự cố -----
    [HttpGet("maintenance-requests")]
    public async Task<ActionResult<List<MaintenanceDto>>> Maintenance([FromQuery] string? status) => Ok(await _support.GetMaintenanceAsync(Uid, Roles.Tenant, status));

    [HttpPost("maintenance-requests")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<MaintenanceDto>> CreateMaintenance([FromForm] CreateMaintenanceDto dto)
        => StatusCode(StatusCodes.Status201Created, await _support.CreateMaintenanceAsync(Uid, dto));

    // ----- Đánh giá, khiếu nại -----
    [HttpGet("reviews")]
    public async Task<ActionResult<List<ReviewDto>>> Reviews() => Ok(await _support.GetTenantReviewsAsync(Uid));

    /// <summary>Đánh giá phòng đã thuê; 403 nếu chưa từng thuê, 409 nếu đã đánh giá.</summary>
    [HttpPost("reviews")]
    public async Task<ActionResult<ReviewDto>> CreateReview(CreateReviewDto dto) => StatusCode(StatusCodes.Status201Created, await _support.CreateReviewAsync(Uid, dto));

    [HttpGet("reports")]
    public async Task<ActionResult<List<ReportDto>>> Reports() => Ok(await _support.GetMyReportsAsync(Uid));

    [HttpPost("reports")]
    public async Task<ActionResult<ReportDto>> CreateReport(CreateReportDto dto) => StatusCode(StatusCodes.Status201Created, await _support.CreateReportAsync(Uid, dto));
}
