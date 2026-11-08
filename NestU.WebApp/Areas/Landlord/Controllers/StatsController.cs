using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using NestU.WebApp.ApiClients;
using NestU.WebApp.Controllers;

namespace NestU.WebApp.Areas.Landlord.Controllers;

[Area("Landlord")]
[Authorize(Roles = "Landlord")]
public class StatsController : BaseController
{
    private readonly LandlordApiClient _api;
    public StatsController(LandlordApiClient api) => _api = api;

    public async Task<IActionResult> Index(int? year, int? houseId)
    {
        var targetYear = year ?? DateTime.Today.Year;

        var houses = await _api.HousesAsync();
        if (!houses.Success) return HandleFailure(houses);

        ViewBag.Houses = new SelectList(houses.Data ?? new(), "Id", "Name", houseId);
        ViewBag.SelectedYear = targetYear;
        ViewBag.SelectedHouseId = houseId;

        // Năm để chọn trong dropdown (3 năm gần nhất)
        var currentYear = DateTime.Today.Year;
        ViewBag.Years = new SelectList(new[] { currentYear, currentYear - 1, currentYear - 2 }, targetYear);

        var r = await _api.StatsAsync(targetYear, houseId);
        if (!r.Success) return HandleFailure(r);

        return View(r.Data);
    }
}

