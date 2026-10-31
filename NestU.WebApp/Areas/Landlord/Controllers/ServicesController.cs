using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using NestU.WebApp.ApiClients;
using NestU.WebApp.Controllers;
using NestU.WebApp.Models.Houses;

namespace NestU.WebApp.Areas.Landlord.Controllers;

[Area("Landlord")]
[Authorize(Roles = "Landlord")]
public class ServicesController : BaseController
{
    private readonly LandlordApiClient _api;
    public ServicesController(LandlordApiClient api) => _api = api;

    public async Task<IActionResult> Index(int? houseId)
    {
        var houses = await _api.HousesAsync();
        if (!houses.Success) return HandleFailure(houses, "Index");
        ViewBag.Houses = new SelectList(houses.Data ?? new(), "Id", "Name", houseId);
        ViewBag.HouseNames = (houses.Data ?? new()).ToDictionary(h => h.Id, h => h.Name);
        var r = await _api.ServicesAsync(houseId);
        ViewBag.Form = new ServiceDto { BoardingHouseId = houseId ?? houses.Data?.FirstOrDefault()?.Id ?? 0 };
        return View(r.Data ?? new());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(ServiceDto m)
    {
        if (!ModelState.IsValid) { TempData["Error"] = "Thông tin dịch vụ chưa hợp lệ."; return RedirectToAction(nameof(Index), new { houseId = m.BoardingHouseId }); }
        if (m.Id == 0) Flash(await _api.CreateServiceAsync(m), "Đã thêm dịch vụ.");
        else Flash(await _api.UpdateServiceAsync(m), "Đã cập nhật dịch vụ.");
        return RedirectToAction(nameof(Index), new { houseId = m.BoardingHouseId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int houseId)
    {
        Flash(await _api.DeleteServiceAsync(id), "Đã xóa dịch vụ.");
        return RedirectToAction(nameof(Index), new { houseId });
    }
}
