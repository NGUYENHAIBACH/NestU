using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NestU.WebApp.ApiClients;
using NestU.WebApp.Controllers;

namespace NestU.WebApp.Areas.Tenant.Controllers;

[Area("Tenant")]
[Authorize(Roles = "Tenant")]
public class DashboardController : BaseController
{
    private readonly TenantApiClient _api;
    public DashboardController(TenantApiClient api) => _api = api;

    public async Task<IActionResult> Index()
    {
        var contracts = await _api.ContractsAsync();
        var invoices = await _api.InvoicesAsync();
        var requests = await _api.RentalRequestsAsync();
        if (contracts.StatusCode == 401) return HandleFailure(contracts);
        ViewBag.Active = contracts.Data?.FirstOrDefault(c => c.Status == "Active");
        ViewBag.UnpaidInvoices = invoices.Data?.Where(i => i.Status != "Paid").ToList() ?? new();
        ViewBag.PendingRequests = requests.Data?.Count(r => r.Status == "Pending") ?? 0;
        return View();
    }
}
