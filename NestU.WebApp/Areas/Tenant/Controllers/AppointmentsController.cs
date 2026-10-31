using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NestU.WebApp.ApiClients;
using NestU.WebApp.Controllers;
using NestU.WebApp.Models.Rentals;

namespace NestU.WebApp.Areas.Tenant.Controllers;

[Area("Tenant")]
[Authorize(Roles = "Tenant")]
public class AppointmentsController : BaseController
{
    private readonly TenantApiClient _api;
    private readonly RoomApiClient _rooms;
    public AppointmentsController(TenantApiClient api, RoomApiClient rooms) { _api = api; _rooms = rooms; }

    public async Task<IActionResult> Index()
    {
        var r = await _api.AppointmentsAsync();
        if (!r.Success) return HandleFailure(r, "Index");
        return View(r.Data);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int roomId)
    {
        var room = await _rooms.DetailAsync(roomId);
        if (!room.Success) { TempData["Error"] = room.Message; return RedirectToAction("Index", "Rooms", new { area = "" }); }
        return View(new CreateAppointmentDto { RoomId = roomId, RoomLabel = $"Phòng {room.Data!.RoomNumber} · {room.Data.HouseName}" });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateAppointmentDto m)
    {
        if (!ModelState.IsValid) return View(m);
        var r = await _api.CreateAppointmentAsync(m);
        if (!r.Success) { r.AddToModelState(ModelState); return View(m); }
        TempData["Success"] = "Đã gửi lịch xem phòng, chờ chủ trọ xác nhận.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        Flash(await _api.CancelAppointmentAsync(id), "Đã hủy lịch xem.");
        return RedirectToAction(nameof(Index));
    }
}
