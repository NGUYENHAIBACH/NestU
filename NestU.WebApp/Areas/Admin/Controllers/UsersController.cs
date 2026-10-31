using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NestU.WebApp.ApiClients;
using NestU.WebApp.Controllers;

namespace NestU.WebApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class UsersController : BaseController
{
    private readonly AdminApiClient _api;
    public UsersController(AdminApiClient api) => _api = api;

    public async Task<IActionResult> Index(string? role, string? status, string? search, int page = 1)
    {
        var r = await _api.UsersAsync(role, status, search, page);
        if (!r.Success) return HandleFailure(r, "Index");
        ViewBag.Role = role; ViewBag.Status = status; ViewBag.Search = search;
        return View(r.Data);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Lock(string id) { Flash(await _api.LockAsync(id), "Đã khóa tài khoản."); return RedirectToAction(nameof(Index)); }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Unlock(string id) { Flash(await _api.UnlockAsync(id), "Đã mở khóa tài khoản."); return RedirectToAction(nameof(Index)); }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetRole(string id, string role) { Flash(await _api.SetRoleAsync(id, role), $"Đã gán role {role}."); return RedirectToAction(nameof(Index)); }
}
