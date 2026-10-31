using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using NestU.WebApp.ApiClients;
using NestU.WebApp.Controllers;
using NestU.WebApp.Models.Billing;

namespace NestU.WebApp.Areas.Landlord.Controllers;

[Area("Landlord")]
[Authorize(Roles = "Landlord")]
public class InvoicesController : BaseController
{
    private readonly LandlordApiClient _api;
    public InvoicesController(LandlordApiClient api) => _api = api;

    public async Task<IActionResult> Index(string? status, int? houseId)
    {
        var houses = await _api.HousesAsync();
        if (!houses.Success) return HandleFailure(houses, "Index");
        ViewBag.Houses = new SelectList(houses.Data ?? new(), "Id", "Name", houseId);
        ViewBag.Status = status; ViewBag.HouseId = houseId;
        var r = await _api.InvoicesAsync(status, houseId);
        return View(r.Data ?? new());
    }

    public async Task<IActionResult> Details(int id)
    {
        var r = await _api.InvoiceAsync(id);
        if (!r.Success) return HandleFailure(r, "Index");
        return View(r.Data);
    }

    [HttpGet]
    public async Task<IActionResult> Generate(int? contractId)
    {
        await LoadContracts();
        return View(new GenerateInvoiceDto { ContractId = contractId ?? 0 });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate(GenerateInvoiceDto m)
    {
        if (!ModelState.IsValid) { await LoadContracts(); return View(m); }
        var r = await _api.GenerateInvoiceAsync(m);
        if (!r.Success) { r.AddToModelState(ModelState); await LoadContracts(); return View(m); }
        TempData["Success"] = $"Đã lập hóa đơn tháng {m.Month}/{m.Year}: {r.Data!.Total:N0}đ.";
        return RedirectToAction(nameof(Details), new { id = r.Data.Id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmPayment(int id) { Flash(await _api.ConfirmPaymentAsync(id), "Đã xác nhận thanh toán."); return RedirectToAction(nameof(Details), new { id }); }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectPayment(int id, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason)) { TempData["Error"] = "Vui lòng nhập lý do."; return RedirectToAction(nameof(Details), new { id }); }
        Flash(await _api.RejectPaymentAsync(id, reason), "Đã từ chối thanh toán, hóa đơn về Chưa thanh toán.");
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task LoadContracts()
    {
        var contracts = await _api.ContractsAsync("Active");
        ViewBag.Contracts = new SelectList((contracts.Data ?? new()).Select(c => new { c.Id, Name = $"P.{c.RoomNumber} · {c.HouseName} · {c.TenantName}" }), "Id", "Name");
    }
}
