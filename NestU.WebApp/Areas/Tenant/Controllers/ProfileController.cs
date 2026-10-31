using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using NestU.WebApp.ApiClients;
using NestU.WebApp.Controllers;
using NestU.WebApp.Models.Rentals;

namespace NestU.WebApp.Areas.Tenant.Controllers;

[Area("Tenant")]
[Authorize(Roles = "Tenant")]
public class ProfileController : BaseController
{
    private readonly TenantApiClient _api;
    private readonly PublicApiClient _public;
    public ProfileController(TenantApiClient api, PublicApiClient @public) { _api = api; _public = @public; }

    public async Task<IActionResult> Index()
    {
        var r = await _api.ProfileAsync();
        if (!r.Success) return HandleFailure(r, "Index");
        await LoadUniversities(r.Data!.UniversityId);
        return View(r.Data);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(TenantProfileDto m)
    {
        if (m.IsStudent && string.IsNullOrWhiteSpace(m.StudentCode)) ModelState.AddModelError(nameof(m.StudentCode), "Sinh viên phải nhập mã số sinh viên");
        if (m.IsStudent && m.UniversityId is null) ModelState.AddModelError(nameof(m.UniversityId), "Sinh viên phải chọn trường");
        if (!ModelState.IsValid) { await LoadUniversities(m.UniversityId); return View(m); }
        var r = await _api.UpdateProfileAsync(m);
        if (!r.Success) { r.AddToModelState(ModelState); await LoadUniversities(m.UniversityId); return View(m); }
        TempData["Success"] = "Đã cập nhật hồ sơ.";
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadUniversities(int? selected)
    {
        var l = await _public.LookupsAsync();
        ViewBag.Universities = new SelectList(l.Data?.Universities ?? new(), "Id", "Name", selected);
    }
}
