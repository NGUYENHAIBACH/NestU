using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NestU.WebApp.ApiClients;
using NestU.WebApp.Controllers;
using NestU.WebApp.Models.Common;

namespace NestU.WebApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class CertificatesController : BaseController
{
    private readonly AdminApiClient _api;
    public CertificatesController(AdminApiClient api) => _api = api;

    public async Task<IActionResult> Index(string? status = "Pending")
    {
        var r = await _api.CertificatesAsync(status);
        if (!r.Success) return HandleFailure(r, "Index");
        ViewBag.Status = status;
        return View(r.Data);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(DecisionModel m)
    {
        if (!m.Approve && string.IsNullOrWhiteSpace(m.Note)) { TempData["Error"] = "Vui lòng ghi lý do từ chối."; return RedirectToAction(nameof(Index)); }
        Flash(await _api.ReviewCertificateAsync(m), m.Approve ? "Đã duyệt PCCC, nhà trọ chuyển sang Đang hoạt động." : "Đã từ chối giấy PCCC.");
        return RedirectToAction(nameof(Index));
    }
}
