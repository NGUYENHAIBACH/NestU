using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NestU.WebApp.ApiClients;
using NestU.WebApp.Controllers;
using NestU.WebApp.Models.Common;

namespace NestU.WebApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class LandlordApprovalsController : BaseController
{
    private readonly AdminApiClient _api;
    public LandlordApprovalsController(AdminApiClient api) => _api = api;

    public async Task<IActionResult> Index(string? status = "Pending")
    {
        var r = await _api.LandlordApprovalsAsync(status);
        if (!r.Success) return HandleFailure(r, "Index");
        ViewBag.Status = status;
        return View(r.Data);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Decide(DecisionModel m)
    {
        if (!m.Approve && string.IsNullOrWhiteSpace(m.Note)) { TempData["Error"] = "Vui lòng ghi lý do từ chối."; return RedirectToAction(nameof(Index)); }
        Flash(await _api.DecideLandlordAsync(m), m.Approve ? "Đã duyệt chủ trọ." : "Đã từ chối hồ sơ.");
        return RedirectToAction(nameof(Index));
    }
}
