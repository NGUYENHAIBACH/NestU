using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NestU.WebApp.ApiClients;
using NestU.WebApp.Controllers;
using NestU.WebApp.Models.Rentals;

namespace NestU.WebApp.Areas.Tenant.Controllers;

[Area("Tenant")]
[Authorize(Roles = "Tenant")]
public class RentalRequestsController : BaseController
{
    private readonly TenantApiClient _api;
    private readonly RoomApiClient _rooms;
    public RentalRequestsController(TenantApiClient api, RoomApiClient rooms) { _api = api; _rooms = rooms; }

    public async Task<IActionResult> Index()
    {
        var r = await _api.RentalRequestsAsync();
        if (!r.Success) return HandleFailure(r, "Index");
        return View(r.Data);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int roomId)
    {
        var room = await _rooms.DetailAsync(roomId);
        if (!room.Success) { TempData["Error"] = room.Message; return RedirectToAction("Index", "Rooms", new { area = "" }); }
        var d = room.Data!;
        return View(new CreateRentalRequestDto { RoomId = roomId, RoomLabel = $"Phòng {d.RoomNumber} · {d.HouseName}", Capacity = d.Capacity, Price = d.Price, DepositAmount = d.DepositAmount });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateRentalRequestDto m)
    {
        if (m.NumberOfOccupants > m.Capacity && m.Capacity > 0) ModelState.AddModelError(nameof(m.NumberOfOccupants), $"Phòng chỉ chứa tối đa {m.Capacity} người");
        if (m.MoveInDate.Date < DateTime.Today) ModelState.AddModelError(nameof(m.MoveInDate), "Ngày dọn vào không được ở quá khứ");
        if (!ModelState.IsValid) return View(m);
        var r = await _api.CreateRentalRequestAsync(m);
        if (!r.Success) { r.AddToModelState(ModelState); return View(m); }
        TempData["Success"] = "Đã gửi yêu cầu thuê, chờ chủ trọ duyệt.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        Flash(await _api.CancelRentalRequestAsync(id), "Đã hủy yêu cầu thuê.");
        return RedirectToAction(nameof(Index));
    }
}
