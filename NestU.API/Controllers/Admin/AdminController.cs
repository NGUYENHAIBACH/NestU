using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NestU.API.DTOs.Admin;
using NestU.API.DTOs.Common;
using NestU.API.DTOs.Houses;
using NestU.API.Helpers;
using NestU.API.Services.Interfaces;

namespace NestU.API.Controllers.Admin;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = Roles.Admin)]
public class AdminController : ControllerBase
{
    private readonly IAdminService _admin;
    public AdminController(IAdminService admin) => _admin = admin;

    // ----- Tài khoản -----
    /// <summary>Danh sách tài khoản. role=Tenant|Landlord|Admin, status=active|locked, search theo email/tên.</summary>
    [HttpGet("users")]
    public async Task<ActionResult<PagedResult<UserListDto>>> Users([FromQuery] string? role, [FromQuery] string? status, [FromQuery] string? search,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => Ok(await _admin.GetUsersAsync(role, status, search, page, pageSize));

    [HttpPut("users/{id}/lock")]
    public async Task<IActionResult> Lock(string id) { await _admin.SetLockAsync(User.GetUserId(), id, true); return NoContent(); }

    [HttpPut("users/{id}/unlock")]
    public async Task<IActionResult> Unlock(string id) { await _admin.SetLockAsync(User.GetUserId(), id, false); return NoContent(); }

    /// <summary>Gán Tenant hoặc Landlord (không gán được Admin).</summary>
    [HttpPut("users/{id}/role")]
    public async Task<IActionResult> SetRole(string id, SetRoleDto dto) { await _admin.SetRoleAsync(id, dto); return NoContent(); }

    // ----- Duyệt chủ trọ -----
    [HttpGet("landlord-approvals")]
    public async Task<ActionResult<List<LandlordApprovalDto>>> LandlordApprovals([FromQuery] string? status) => Ok(await _admin.GetLandlordApprovalsAsync(status));

    [HttpPut("landlord-approvals/{id:int}/decision")]
    public async Task<IActionResult> DecideLandlord(int id, DecisionDto dto) { await _admin.DecideLandlordAsync(id, dto); return NoContent(); }

    // ----- Duyệt PCCC -----
    [HttpGet("certificates")]
    public async Task<ActionResult<List<CertificateDto>>> Certificates([FromQuery] string? status) => Ok(await _admin.GetCertificatesAsync(status));

    [HttpGet("certificates/{id:int}")]
    public async Task<ActionResult<CertificateDto>> Certificate(int id) => Ok(await _admin.GetCertificateAsync(id));

    /// <summary>Duyệt (nhà trọ → Active) hoặc từ chối (kèm lý do).</summary>
    [HttpPut("certificates/{id:int}/review")]
    public async Task<IActionResult> ReviewCertificate(int id, ReviewCertificateDto dto) { await _admin.ReviewCertificateAsync(id, dto); return NoContent(); }

    // ----- Khiếu nại -----
    // ----- Nhà trọ (ẩn/hiện tin) -----
    [HttpGet("houses")]
    public async Task<ActionResult<List<AdminHouseDto>>> Houses() => Ok(await _admin.GetHousesAsync());

    [HttpPut("houses/{id:int}/hidden")]
    public async Task<IActionResult> SetHouseHidden(int id, SetHouseHiddenDto dto) { await _admin.SetHouseHiddenAsync(id, dto); return NoContent(); }

    [HttpGet("reports")]
    public async Task<ActionResult<List<ReportDto>>> Reports([FromQuery] string? status) => Ok(await _admin.GetReportsAsync(status));

    [HttpPut("reports/{id:int}/resolve")]
    public async Task<IActionResult> ResolveReport(int id, ResolveReportDto dto) { await _admin.ResolveReportAsync(id, dto); return NoContent(); }

    // ----- Danh mục: areas | amenities | asset-types | universities -----
    [HttpGet("catalog/{type}")]
    public async Task<ActionResult<List<CatalogItemDto>>> Catalog(string type) => Ok(await _admin.GetCatalogAsync(type));

    [HttpPost("catalog/{type}")]
    public async Task<ActionResult<CatalogItemDto>> CreateCatalog(string type, CatalogItemDto dto)
        => StatusCode(StatusCodes.Status201Created, await _admin.CreateCatalogAsync(type, dto));

    [HttpPut("catalog/{type}/{id:int}")]
    public async Task<IActionResult> UpdateCatalog(string type, int id, CatalogItemDto dto) { await _admin.UpdateCatalogAsync(type, id, dto); return NoContent(); }

    [HttpDelete("catalog/{type}/{id:int}")]
    public async Task<IActionResult> DeleteCatalog(string type, int id) { await _admin.DeleteCatalogAsync(type, id); return NoContent(); }

    // ----- Thống kê -----
    [HttpGet("stats")]
    public async Task<ActionResult<AdminStatsDto>> Stats() => Ok(await _admin.GetStatsAsync());
}
