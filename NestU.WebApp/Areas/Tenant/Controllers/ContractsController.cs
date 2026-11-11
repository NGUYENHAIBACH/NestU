using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NestU.WebApp.ApiClients;
using NestU.WebApp.Controllers;
using NestU.WebApp.Models.Rentals;

namespace NestU.WebApp.Areas.Tenant.Controllers;

[Area("Tenant")]
[Authorize(Roles = "Tenant")]
public class ContractsController : BaseController
{
    private readonly TenantApiClient _api;
    public ContractsController(TenantApiClient api) => _api = api;

    public async Task<IActionResult> Index()
    {
        var r = await _api.ContractsAsync();
        if (!r.Success) return HandleFailure(r, "Index");
        var changes = await _api.ChangeRequestsAsync();
        ViewBag.ChangeRequests = changes.Data ?? new();
        return View(r.Data);
    }

    public async Task<IActionResult> Details(int id, string tab = "info")
    {
        var r = await _api.ContractAsync(id);
        if (!r.Success) return HandleFailure(r, "Index");
        var assets = await _api.ContractAssetsAsync(id);
        ViewBag.Assets = assets.Data ?? new();
        ViewBag.Tab = tab;
        if (tab == "deposit" && r.Data!.DepositPaidAt == null && r.Data.DepositAmount > 0)
        {
            var qr = await _api.DepositQrAsync(id);
            if (qr.Success) ViewBag.DepositQr = qr.Data;
        }
        return View(r.Data);
    }

    [HttpGet]
    public async Task<IActionResult> CheckDepositStatus(int id)
    {
        var r = await _api.ContractAsync(id);
        if (!r.Success) return Json(new { success = false });
        return Json(new { success = true, isPaid = r.Data!.DepositPaidAt != null });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmAssets(int id)
    {
        Flash(await _api.ConfirmAssetsAsync(id), "Đã xác nhận biên bản nhận phòng.");
        return RedirectToAction(nameof(Details), new { id, tab = "assets" });
    }

    [HttpGet]
    public IActionResult Change(int id, string type) => View(new CreateContractChangeDto { ContractId = id, Type = type });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Change(CreateContractChangeDto m)
    {
        if (m.Type == "Renew" && m.NewEndDate is null) ModelState.AddModelError(nameof(m.NewEndDate), "Vui lòng chọn ngày kết thúc mới");
        if (!ModelState.IsValid) return View(m);
        var r = await _api.CreateChangeAsync(m);
        if (!r.Success) { r.AddToModelState(ModelState); return View(m); }
        TempData["Success"] = m.Type == "Renew" ? "Đã gửi yêu cầu gia hạn." : "Đã gửi yêu cầu trả phòng.";
        return RedirectToAction(nameof(Details), new { id = m.ContractId });
    }
}
