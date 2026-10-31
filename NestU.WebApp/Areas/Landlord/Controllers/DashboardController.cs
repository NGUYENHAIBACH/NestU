using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NestU.WebApp.ApiClients;
using NestU.WebApp.Controllers;

namespace NestU.WebApp.Areas.Landlord.Controllers;

[Area("Landlord")]
[Authorize(Roles = "Landlord")]
public class DashboardController : BaseController
{
    private readonly LandlordApiClient _api;
    public DashboardController(LandlordApiClient api) => _api = api;

    public async Task<IActionResult> Index()
    {
        var profile = await _api.ProfileAsync();
        if (profile.StatusCode == 401) return HandleFailure(profile);
        ViewBag.Profile = profile.Data;
        if (profile.Data?.ApprovalStatus != "Approved") return View();

        var houses = await _api.HousesAsync();
        var requests = await _api.RentalRequestsAsync("Pending");
        var appts = await _api.AppointmentsAsync("Pending");
        var invoices = await _api.InvoicesAsync();
        var maint = await _api.MaintenanceAsync("Open");
        ViewBag.Houses = houses.Data ?? new();
        ViewBag.PendingRequests = requests.Data?.Count ?? 0;
        ViewBag.PendingAppointments = appts.Data?.Count ?? 0;
        ViewBag.PendingConfirm = invoices.Data?.Count(i => i.Status == "PendingConfirm") ?? 0;
        ViewBag.Overdue = invoices.Data?.Count(i => i.DisplayStatus == "Overdue") ?? 0;
        ViewBag.OpenMaintenance = maint.Data?.Count ?? 0;
        return View();
    }
}
