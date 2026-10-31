using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NestU.WebApp.ApiClients;
using NestU.WebApp.Controllers;
using NestU.WebApp.Models.Support;

namespace NestU.WebApp.Areas.Landlord.Controllers;

[Area("Landlord")]
[Authorize(Roles = "Landlord")]
public class MaintenanceController : BaseController
{
    private readonly LandlordApiClient _api;
    public MaintenanceController(LandlordApiClient api) => _api = api;

    public async Task<IActionResult> Index(string? status)
    {
        var r = await _api.MaintenanceAsync(status);
        if (!r.Success) return HandleFailure(r, "Index");
        ViewBag.Status = status;
        return View(r.Data);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(UpdateMaintenanceStatusDto m)
    {
        Flash(await _api.UpdateMaintenanceAsync(m), "Đã cập nhật tiến độ xử lý.");
        return RedirectToAction(nameof(Index));
    }
}
