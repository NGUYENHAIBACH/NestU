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

    /// <summary>Form gọi để biết chỉ số kỳ trước/kỳ sau khi chọn hợp đồng và tháng.</summary>
    [HttpGet]
    public async Task<IActionResult> Bounds(int contractId, int month, int year)
    {
        var r = await _api.ReadingBoundsAsync(contractId, month, year);
        if (!r.Success) return Json(new { success = false, message = r.Message });
        return Json(new { success = true, data = r.Data });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(UtilityReadingDto m)
    {
        if (!ModelState.IsValid) { TempData["Error"] = "Thông tin chỉ số chưa hợp lệ."; return RedirectToAction(nameof(Index)); }
        // Kiểm tra trước ở Web App để báo lỗi rõ; API vẫn kiểm tra lại khi ghi
        var b = await _api.ReadingBoundsAsync(m.ContractId, m.Month, m.Year);
        if (b.Success && b.Data is not null)
        {
            string? err = null;
            if (m.ElectricIndex < b.Data.PreviousElectricIndex) err = $"Chỉ số điện ({m.ElectricIndex}) phải lớn hơn hoặc bằng chỉ số {b.Data.PreviousLabel} ({b.Data.PreviousElectricIndex}).";
            else if (m.WaterIndex < b.Data.PreviousWaterIndex) err = $"Chỉ số nước ({m.WaterIndex}) phải lớn hơn hoặc bằng chỉ số {b.Data.PreviousLabel} ({b.Data.PreviousWaterIndex}).";
            else if (b.Data.NextElectricIndex is not null && m.ElectricIndex > b.Data.NextElectricIndex) err = $"Chỉ số điện ({m.ElectricIndex}) không được lớn hơn chỉ số {b.Data.NextLabel} ({b.Data.NextElectricIndex}).";
            else if (b.Data.NextWaterIndex is not null && m.WaterIndex > b.Data.NextWaterIndex) err = $"Chỉ số nước ({m.WaterIndex}) không được lớn hơn chỉ số {b.Data.NextLabel} ({b.Data.NextWaterIndex}).";
            if (err is not null) { TempData["Error"] = err; return RedirectToAction(nameof(Index)); }
        }
        Flash(await _api.AddReadingAsync(m), $"Đã ghi chỉ số tháng {m.Month}/{m.Year}.");
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadContracts()
    {
        var contracts = await _api.ContractsAsync("Active");
        ViewBag.Contracts = new SelectList((contracts.Data ?? new()).Select(c => new { c.Id, Name = $"P.{c.RoomNumber} · {c.HouseName} · {c.TenantName}" }), "Id", "Name");
    }
}
