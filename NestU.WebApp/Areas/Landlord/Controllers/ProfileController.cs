using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NestU.WebApp.ApiClients;
using NestU.WebApp.Controllers;

namespace NestU.WebApp.Areas.Landlord.Controllers;

[Area("Landlord")]
[Authorize(Roles = "Landlord")]
public class ProfileController : BaseController
{
    private readonly LandlordApiClient _api;
    public ProfileController(LandlordApiClient api) => _api = api;

    public async Task<IActionResult> Index()
    {
        var r = await _api.ProfileAsync();
        if (!r.Success) return HandleFailure(r, "Index");
        return View(r.Data);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(LandlordProfileDto m)
    {
        if (!ModelState.IsValid) return View(m);
        var r = await _api.UpdateProfileAsync(m);
        if (!r.Success) { r.AddToModelState(ModelState); return View(m); }
        TempData["Success"] = "Đã cập nhật hồ sơ.";
        return RedirectToAction(nameof(Index));
    }
}
