using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NestU.WebApp.ApiClients;
using NestU.WebApp.Controllers;

namespace NestU.WebApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class DashboardController : BaseController
{
    private readonly AdminApiClient _api;
    public DashboardController(AdminApiClient api) => _api = api;

    public async Task<IActionResult> Index()
    {
        var r = await _api.StatsAsync();
        if (!r.Success) return HandleFailure(r, "Index");
        return View(r.Data);
    }
}
