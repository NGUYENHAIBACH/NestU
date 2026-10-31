using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NestU.WebApp.ApiClients;
using NestU.WebApp.Controllers;
using NestU.WebApp.Models.Support;

namespace NestU.WebApp.Areas.Tenant.Controllers;

[Area("Tenant")]
[Authorize(Roles = "Tenant")]
public class ReviewsController : BaseController
{
    private readonly TenantApiClient _api;
    public ReviewsController(TenantApiClient api) => _api = api;

    public async Task<IActionResult> Index()
    {
        var r = await _api.ReviewsAsync();
        if (!r.Success) return HandleFailure(r, "Index");
        var contracts = await _api.ContractsAsync();
        var reviewed = r.Data!.Select(x => x.ContractId).ToHashSet();
        ViewBag.Reviewable = contracts.Data?.Where(c => !reviewed.Contains(c.Id) && c.StartDate <= DateTime.Today).ToList() ?? new();
        return View(r.Data);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int contractId)
    {
        var c = await _api.ContractAsync(contractId);
        if (!c.Success) return HandleFailure(c, "Index");
        return View(new CreateReviewDto { ContractId = contractId, RoomLabel = $"Phòng {c.Data!.RoomNumber} · {c.Data.HouseName}" });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateReviewDto m)
    {
        if (!ModelState.IsValid) return View(m);
        var r = await _api.CreateReviewAsync(m);
        if (!r.Success) { r.AddToModelState(ModelState); return View(m); }
        TempData["Success"] = "Cảm ơn bạn đã đánh giá.";
        return RedirectToAction(nameof(Index));
    }
}
