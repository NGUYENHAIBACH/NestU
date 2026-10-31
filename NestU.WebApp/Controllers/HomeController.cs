using Microsoft.AspNetCore.Mvc;
using NestU.WebApp.ApiClients;

namespace NestU.WebApp.Controllers;

public class HomeController : Controller
{
    private readonly PublicApiClient _public;
    private readonly RoomApiClient _rooms;

    public HomeController(PublicApiClient @public, RoomApiClient rooms)
    {
        _public = @public;
        _rooms = rooms;
    }

    public async Task<IActionResult> Index()
    {
        var stats = await _public.StatsAsync();
        var featured = await _rooms.FeaturedAsync(6);
        ViewBag.Stats = stats.Data;
        ViewBag.ApiDown = !stats.Success && stats.StatusCode >= 500;
        return View(featured.Data ?? new());
    }

    [Route("/error/{code:int}")]
    public IActionResult Error(int code)
    {
        ViewBag.Code = code;
        return View();
    }
}
