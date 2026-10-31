using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using NestU.WebApp.ApiClients;
using NestU.WebApp.Controllers;
using NestU.WebApp.Models.Assets;
using NestU.WebApp.Models.Common;
using NestU.WebApp.Models.Rentals;

namespace NestU.WebApp.Areas.Landlord.Controllers;

[Area("Landlord")]
[Authorize(Roles = "Landlord")]
public class ContractsController : BaseController
{
    private readonly LandlordApiClient _api;
    public ContractsController(LandlordApiClient api) => _api = api;

    public async Task<IActionResult> Index(string? status, int? houseId)
    {
        var houses = await _api.HousesAsync();
        if (!houses.Success) return HandleFailure(houses, "Index");
        ViewBag.Houses = new SelectList(houses.Data ?? new(), "Id", "Name", houseId);
        ViewBag.Status = status; ViewBag.HouseId = houseId;
        var r = await _api.ContractsAsync(status, houseId);
        var changes = await _api.ChangeRequestsAsync("Pending");
        ViewBag.PendingChanges = changes.Data ?? new();
        return View(r.Data ?? new());
    }

    public async Task<IActionResult> Details(int id, string tab = "info")
    {
        var r = await _api.ContractAsync(id);
        if (!r.Success) return HandleFailure(r, "Index");
        var assets = await _api.ContractAssetsAsync(id);
        ViewBag.Assets = assets.Data ?? new();
        ViewBag.Tab = tab;
        var invoices = await _api.InvoicesAsync(null, r.Data!.HouseId);
        ViewBag.Invoices = (invoices.Data ?? new()).Where(i => i.ContractId == id).ToList();
        return View(r.Data);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Extend(ExtendContractDto m)
    {
        Flash(await _api.ExtendAsync(m), "Đã gia hạn hợp đồng.");
        return RedirectToAction(nameof(Details), new { id = m.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Checkout(int id)
    {
        var assets = await _api.ContractAssetsAsync(id);
        if (!assets.Success) return HandleFailure(assets, "Index");
        var checkIn = assets.Data!.Where(a => a.Stage == "CheckIn").ToList();
        var checkOut = assets.Data!.Where(a => a.Stage == "CheckOut").ToDictionary(a => a.RoomAssetId);
        var m = new CheckoutAssetsDto
        {
            ContractId = id,
            Items = checkIn.Select(a => new CheckoutAssetItemDto
            {
                RoomAssetId = a.RoomAssetId, Name = a.Name, CheckInQuantity = a.Quantity, CheckInCondition = a.Condition,
                Quantity = checkOut.TryGetValue(a.RoomAssetId, out var co) ? co.Quantity : a.Quantity,
                Condition = co?.Condition ?? a.Condition, DeductionAmount = co?.DeductionAmount ?? 0, Note = co?.Note
            }).ToList()
        };
        return View(m);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(CheckoutAssetsDto m)
    {
        if (!ModelState.IsValid) return View(m);
        var r = await _api.CheckoutAsync(m);
        if (!r.Success) { r.AddToModelState(ModelState); return View(m); }
        TempData["Success"] = "Đã lưu biên bản trả phòng. Bước tiếp theo: quyết toán cọc.";
        return RedirectToAction(nameof(Details), new { id = m.ContractId, tab = "assets" });
    }

    [HttpGet]
    public async Task<IActionResult> Settle(int id)
    {
        var c = await _api.ContractAsync(id);
        if (!c.Success) return HandleFailure(c, "Index");
        var assets = await _api.ContractAssetsAsync(id, "CheckOut");
        ViewBag.Contract = c.Data;
        ViewBag.Damage = (assets.Data ?? new()).Sum(a => a.DeductionAmount);
        var invoices = await _api.InvoicesAsync(null, c.Data!.HouseId);
        ViewBag.Unpaid = (invoices.Data ?? new()).Where(i => i.ContractId == id && i.Status != "Paid").Sum(i => i.Total);
        return View(new CreateDepositSettlementDto { ContractId = id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Settle(CreateDepositSettlementDto m)
    {
        var r = await _api.SettleAsync(m);
        if (!r.Success) { TempData["Error"] = r.Message; return RedirectToAction(nameof(Settle), new { id = m.ContractId }); }
        TempData["Success"] = $"Đã quyết toán cọc: hoàn {r.Data!.RefundAmount:N0}đ. Hợp đồng kết thúc, phòng trở lại Còn trống.";
        return RedirectToAction(nameof(Details), new { id = m.ContractId, tab = "deposit" });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DecideChange(DecisionModel m)
    {
        if (!m.Approve && string.IsNullOrWhiteSpace(m.Note)) { TempData["Error"] = "Vui lòng nhập lý do từ chối."; return RedirectToAction(nameof(Index)); }
        Flash(await _api.DecideChangeAsync(m), m.Approve ? "Đã chấp nhận yêu cầu." : "Đã từ chối yêu cầu.");
        return RedirectToAction(nameof(Index));
    }
}
