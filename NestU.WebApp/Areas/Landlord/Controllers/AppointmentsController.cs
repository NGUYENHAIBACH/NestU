using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NestU.WebApp.ApiClients;
using NestU.WebApp.Controllers;

namespace NestU.WebApp.Areas.Landlord.Controllers;

[Area("Landlord")]
[Authorize(Roles = "Landlord")]
public class AppointmentsController : BaseController
{
    private readonly LandlordApiClient _api;
    public AppointmentsController(LandlordApiClient api) => _api = api;

    public async Task<IActionResult> Index(string? status)
    {
        var r = await _api.AppointmentsAsync(status);
        if (!r.Success) return HandleFailure(r, "Index");
        ViewBag.Status = status;
        return View(r.Data);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(int id) { Flash(await _api.ConfirmAppointmentAsync(id), "Đã xác nhận lịch xem."); return RedirectToAction(nameof(Index)); }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason)) { TempData["Error"] = "Vui lòng nhập lý do từ chối."; return RedirectToAction(nameof(Index)); }
        Flash(await _api.RejectAppointmentAsync(id, reason), "Đã từ chối lịch xem.");
        return RedirectToAction(nameof(Index));
    }
}
