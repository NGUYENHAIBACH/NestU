using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NestU.WebApp.ApiClients;
using NestU.WebApp.Controllers;
using NestU.WebApp.Models.Admin;

namespace NestU.WebApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class CatalogController : BaseController
{
    private readonly AdminApiClient _api;
    public CatalogController(AdminApiClient api) => _api = api;

    public static readonly Dictionary<string, string> Types = new()
    {
        ["areas"] = "Khu vực", ["amenities"] = "Tiện ích", ["asset-types"] = "Loại tài sản", ["universities"] = "Trường đại học"
    };

    public async Task<IActionResult> Index(string type = "areas")
    {
        if (!Types.ContainsKey(type)) type = "areas";
        var r = await _api.CatalogAsync(type);
        if (!r.Success) return HandleFailure(r, "Index");
        ViewBag.Type = type;
        return View(r.Data);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(string type, CatalogItemDto m)
    {
        if (string.IsNullOrWhiteSpace(m.Name)) { TempData["Error"] = "Tên là bắt buộc."; return RedirectToAction(nameof(Index), new { type }); }
        if (m.Id == 0) Flash(await _api.CreateCatalogAsync(type, m), "Đã thêm.");
        else Flash(await _api.UpdateCatalogAsync(type, m), "Đã cập nhật.");
        return RedirectToAction(nameof(Index), new { type });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string type, int id)
    {
        Flash(await _api.DeleteCatalogAsync(type, id), "Đã xóa.");
        return RedirectToAction(nameof(Index), new { type });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MergeArea(int id, int targetId)
    {
        var r = await _api.MergeAreaAsync(id, targetId);
        if (r.Success) TempData["Success"] = $"Đã gộp vào \"{r.Data!.TargetName}\", chuyển {r.Data.MovedHouses} nhà trọ.";
        else TempData["Error"] = r.Message;
        return RedirectToAction(nameof(Index), new { type = "areas" });
    }
}
