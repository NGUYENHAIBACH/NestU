using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NestU.WebApp.ApiClients;
using NestU.WebApp.Controllers;

namespace NestU.WebApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class HousesController : BaseController
{
    private readonly AdminApiClient _api;
    public HousesController(AdminApiClient api) => _api = api;

    public async Task<IActionResult> Index()
    {
        var r = await _api.HousesAsync();
        if (!r.Success) return HandleFailure(r, "Index");
        return View(r.Data);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetHidden(int id, bool hidden)
    {
        Flash(await _api.SetHouseHiddenAsync(id, hidden), hidden ? "Đã ẩn tin nhà trọ." : "Đã hiện lại tin nhà trọ.");
        return RedirectToAction(nameof(Index));
    }
}
