using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NestU.WebApp.ApiClients;
using NestU.WebApp.Controllers;
using NestU.WebApp.Models.Billing;

namespace NestU.WebApp.Areas.Tenant.Controllers;

[Area("Tenant")]
[Authorize(Roles = "Tenant")]
public class InvoicesController : BaseController
{
    private readonly TenantApiClient _api;
    public InvoicesController(TenantApiClient api) => _api = api;

    public async Task<IActionResult> Index()
    {
        var r = await _api.InvoicesAsync();
        if (!r.Success) return HandleFailure(r, "Index");
        return View(r.Data);
    }

    public async Task<IActionResult> Details(int id)
    {
        var r = await _api.InvoiceAsync(id);
        if (!r.Success) return HandleFailure(r, "Index");
        var qr = await _api.QrAsync(id);
        ViewBag.Qr = qr.Success ? qr.Data : null;
        return View(r.Data);
    }

    [HttpGet]
    public async Task<IActionResult> CheckStatus(int id)
    {
        var r = await _api.InvoiceAsync(id);
        if (!r.Success) return Json(new { success = false });
        return Json(new { success = true, isPaid = r.Data!.Status == "Paid" });
    }

    [HttpGet]
    public async Task<IActionResult> Pay(int id)
    {
        var r = await _api.InvoiceAsync(id);
        if (!r.Success) return HandleFailure(r, "Index");
        ViewBag.Invoice = r.Data;
        return View(new ReportPaymentDto { InvoiceId = id, Amount = r.Data!.RemainingAmount });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Pay(ReportPaymentDto m)
    {
        if (!ModelState.IsValid) { ViewBag.Invoice = (await _api.InvoiceAsync(m.InvoiceId)).Data; return View(m); }
        var r = await _api.ReportPaymentAsync(m);
        if (!r.Success) { r.AddToModelState(ModelState); ViewBag.Invoice = (await _api.InvoiceAsync(m.InvoiceId)).Data; return View(m); }
        TempData["Success"] = "Đã báo thanh toán, chờ chủ trọ xác nhận.";
        return RedirectToAction(nameof(Details), new { id = m.InvoiceId });
    }
}
