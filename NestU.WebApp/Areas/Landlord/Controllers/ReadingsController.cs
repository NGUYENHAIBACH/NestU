using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using NestU.WebApp.ApiClients;
using NestU.WebApp.Controllers;
using NestU.WebApp.Models.Billing;

namespace NestU.WebApp.Areas.Landlord.Controllers;

[Area("Landlord")]
[Authorize(Roles = "Landlord")]
public class ReadingsController : BaseController
{
    private readonly LandlordApiClient _api;
    public ReadingsController(LandlordApiClient api) => _api = api;

    public async Task<IActionResult> Index(int? houseId)
    {
        var r = await _api.ReadingsAsync(houseId);
        if (!r.Success) return HandleFailure(r, "Index");
        await LoadContracts();
        ViewBag.Form = new UtilityReadingDto();
        return View(r.Data);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(UtilityReadingDto m)
    {
        if (!ModelState.IsValid) { TempData["Error"] = "Thông tin chỉ số chưa hợp lệ."; return RedirectToAction(nameof(Index)); }
        Flash(await _api.AddReadingAsync(m), $"Đã ghi chỉ số tháng {m.Month}/{m.Year}.");
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadContracts()
    {
        var contracts = await _api.ContractsAsync("Active");
        ViewBag.Contracts = new SelectList((contracts.Data ?? new()).Select(c => new { c.Id, Name = $"P.{c.RoomNumber} · {c.HouseName} · {c.TenantName}" }), "Id", "Name");
    }
}
