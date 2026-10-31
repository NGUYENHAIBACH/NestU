using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NestU.WebApp.ApiClients;
using NestU.WebApp.Controllers;
using NestU.WebApp.Models.Rentals;

namespace NestU.WebApp.Areas.Landlord.Controllers;

[Area("Landlord")]
[Authorize(Roles = "Landlord")]
public class RentalRequestsController : BaseController
{
    private readonly LandlordApiClient _api;
    public RentalRequestsController(LandlordApiClient api) => _api = api;

    public async Task<IActionResult> Index(string? status)
    {
        var r = await _api.RentalRequestsAsync(status);
        if (!r.Success) return HandleFailure(r, "Index");
        ViewBag.Status = status;
        return View(r.Data);
    }

    [HttpGet]
    public async Task<IActionResult> Approve(int id)
    {
        var all = await _api.RentalRequestsAsync();
        var req = all.Data?.FirstOrDefault(x => x.Id == id);
        if (req is null) { TempData["Error"] = "Không tìm thấy yêu cầu."; return RedirectToAction(nameof(Index)); }
        var rooms = await _api.RoomsAsync();
        var houseId = rooms.Data?.FirstOrDefault(x => x.Id == req.RoomId)?.HouseId;
        var services = houseId is null ? null : await _api.ServicesAsync(houseId);
        var m = new ApproveRentalDto
        {
            RequestId = id, Request = req, StartDate = req.MoveInDate, DurationMonths = req.DurationMonths, DepositAmount = req.DepositAmount,
            AvailableServices = services?.Data?.Where(s => s.IsActive).ToList() ?? new()
        };
        for (var i = 1; i < req.NumberOfOccupants; i++) m.Members.Add(new ContractMemberDto());
        return View(m);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(ApproveRentalDto m)
    {
        m.Members = m.Members.Where(x => !string.IsNullOrWhiteSpace(x.FullName) || !string.IsNullOrWhiteSpace(x.CitizenId)).ToList();
        ModelState.Clear(); TryValidateModel(m);
        if (!ModelState.IsValid) return await Reload(m);
        var r = await _api.ApproveRequestAsync(m);
        if (!r.Success) { r.AddToModelState(ModelState); return await Reload(m); }
        TempData["Success"] = $"Đã duyệt. Hợp đồng #{r.Data!.Id} được tạo, phòng chuyển sang Đã thuê.";
        return RedirectToAction("Details", "Contracts", new { id = r.Data.Id });
    }

    private async Task<IActionResult> Reload(ApproveRentalDto m)
    {
        var all = await _api.RentalRequestsAsync();
        m.Request = all.Data?.FirstOrDefault(x => x.Id == m.RequestId);
        var rooms = await _api.RoomsAsync();
        var houseId = rooms.Data?.FirstOrDefault(x => x.Id == m.Request?.RoomId)?.HouseId;
        var services = houseId is null ? null : await _api.ServicesAsync(houseId);
        m.AvailableServices = services?.Data?.Where(s => s.IsActive).ToList() ?? new();
        return View(m);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason)) { TempData["Error"] = "Vui lòng nhập lý do từ chối."; return RedirectToAction(nameof(Index)); }
        Flash(await _api.RejectRequestAsync(id, reason), "Đã từ chối yêu cầu thuê.");
        return RedirectToAction(nameof(Index));
    }
}
