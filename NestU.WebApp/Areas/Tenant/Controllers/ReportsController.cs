using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NestU.WebApp.ApiClients;
using NestU.WebApp.Controllers;
using NestU.WebApp.Models.Support;

namespace NestU.WebApp.Areas.Tenant.Controllers;

[Area("Tenant")]
[Authorize(Roles = "Tenant")]
public class ReportsController : BaseController
{
    private readonly TenantApiClient _api;
    private readonly RoomApiClient _rooms;
    public ReportsController(TenantApiClient api, RoomApiClient rooms) { _api = api; _rooms = rooms; }

    public async Task<IActionResult> Index()
    {
        var r = await _api.ReportsAsync();
        if (!r.Success) return HandleFailure(r, "Index");
        return View(r.Data);
    }

    [HttpGet]
    public async Task<IActionResult> Create(string targetType = "Room", int targetId = 0)
    {
        var m = new CreateReportDto { TargetType = targetType, TargetId = targetId };
        if (targetType == "Room" && targetId > 0)
        {
            var room = await _rooms.DetailAsync(targetId);
            if (room.Success) m.TargetLabel = $"Phòng {room.Data!.RoomNumber} · {room.Data.HouseName}";
        }
        else if (targetType == "House" && targetId > 0)
        {
            var house = await _rooms.HouseAsync(targetId);
            if (house.Success) m.TargetLabel = house.Data!.Name;
        }
        return View(m);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateReportDto m)
    {
        if (!ModelState.IsValid) return View(m);
        var r = await _api.CreateReportAsync(m);
        if (!r.Success) { r.AddToModelState(ModelState); return View(m); }
        TempData["Success"] = "Đã gửi khiếu nại tới quản trị viên.";
        return RedirectToAction(nameof(Index));
    }
}
