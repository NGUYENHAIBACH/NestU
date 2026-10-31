using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using NestU.WebApp.ApiClients;
using NestU.WebApp.Controllers;
using NestU.WebApp.Models.Support;

namespace NestU.WebApp.Areas.Tenant.Controllers;

[Area("Tenant")]
[Authorize(Roles = "Tenant")]
public class MaintenanceController : BaseController
{
    private readonly TenantApiClient _api;
    public MaintenanceController(TenantApiClient api) => _api = api;

    public async Task<IActionResult> Index()
    {
        var r = await _api.MaintenanceAsync();
        if (!r.Success) return HandleFailure(r, "Index");
        return View(r.Data);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        if (!await LoadActiveContract()) return RedirectToAction(nameof(Index));
        return View(new CreateMaintenanceDto { ContractId = (int)ViewBag.ContractId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateMaintenanceDto m)
    {
        if (!ModelState.IsValid) { await LoadActiveContract(); return View(m); }
        var r = await _api.CreateMaintenanceAsync(m);
        if (!r.Success) { r.AddToModelState(ModelState); await LoadActiveContract(); return View(m); }
        TempData["Success"] = "Đã gửi báo sự cố.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> LoadActiveContract()
    {
        var contracts = await _api.ContractsAsync();
        var active = contracts.Data?.FirstOrDefault(c => c.Status == "Active");
        if (active is null) { TempData["Error"] = "Bạn chưa có hợp đồng đang hiệu lực."; return false; }
        ViewBag.ContractId = active.Id;
        ViewBag.ContractLabel = $"Phòng {active.RoomNumber} · {active.HouseName}";
        var assets = await _api.ContractAssetsAsync(active.Id, "CheckIn");
        ViewBag.Assets = new SelectList(assets.Data ?? new(), "RoomAssetId", "Name");
        return true;
    }
}
