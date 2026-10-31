using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using NestU.WebApp.ApiClients;
using NestU.WebApp.Models.Rooms;

namespace NestU.WebApp.Controllers;

public class RoomsController : BaseController
{
    private readonly RoomApiClient _rooms;
    private readonly PublicApiClient _public;

    public RoomsController(RoomApiClient rooms, PublicApiClient @public)
    {
        _rooms = rooms;
        _public = @public;
    }

    /// <summary>Tìm phòng — bộ lọc dịch sang OData $filter/$orderby/$top/$skip (mục 1.9).</summary>
    public async Task<IActionResult> Index(RoomSearchModel search)
    {
        var lookups = await _public.LookupsAsync();
        ViewBag.Areas = new SelectList(lookups.Data?.Areas ?? new(), "Id", "Name", search.AreaId);
        var result = await _rooms.SearchAsync(search);
        if (!result.Success) TempData["Error"] = result.Message;
        ViewBag.Total = result.Data?.Count ?? 0;
        ViewBag.Search = search;
        return View(result.Data?.Value ?? new());
    }

    public async Task<IActionResult> Details(int id)
    {
        var r = await _rooms.DetailAsync(id);
        if (!r.Success || r.Data is null) { TempData["Error"] = r.Message; return RedirectToAction(nameof(Index)); }
        var reviews = await _rooms.ReviewsAsync(id);
        ViewBag.Reviews = reviews.Data ?? new();
        return View(r.Data);
    }

    public async Task<IActionResult> House(int id)
    {
        var r = await _rooms.HouseAsync(id);
        if (!r.Success || r.Data is null) { TempData["Error"] = r.Message; return RedirectToAction(nameof(Index)); }
        return View(r.Data);
    }
}
