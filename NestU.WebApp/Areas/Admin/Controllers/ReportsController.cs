using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NestU.WebApp.ApiClients;
using NestU.WebApp.Controllers;
using NestU.WebApp.Models.Admin;

namespace NestU.WebApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ReportsController : BaseController
{
    private readonly AdminApiClient _api;
    public ReportsController(AdminApiClient api) => _api = api;

    public async Task<IActionResult> Index(string? status = "Open")
    {
        var r = await _api.ReportsAsync(status);
        if (!r.Success) return HandleFailure(r, "Index");
        ViewBag.Status = status;
        return View(r.Data);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Resolve(ResolveReportDto m)
    {
        Flash(await _api.ResolveReportAsync(m), "Đã xử lý khiếu nại.");
        return RedirectToAction(nameof(Index));
    }
}
