using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using NestU.WebApp.ApiClients;
using NestU.WebApp.Controllers;
using NestU.WebApp.Models.Assets;
using NestU.WebApp.Models.Rooms;

namespace NestU.WebApp.Areas.Landlord.Controllers;

[Area("Landlord")]
[Authorize(Roles = "Landlord")]
public class RoomsController : BaseController
{
    private readonly LandlordApiClient _api;
    private readonly PublicApiClient _public;
    public RoomsController(LandlordApiClient api, PublicApiClient @public) { _api = api; _public = @public; }

    public async Task<IActionResult> Index(int? houseId, int? floor)
    {
        var houses = await _api.HousesAsync();
        if (!houses.Success) return HandleFailure(houses, "Index");
        ViewBag.Houses = new SelectList(houses.Data ?? new(), "Id", "Name", houseId);
        var rooms = await _api.RoomsAsync(houseId, floor);
        ViewBag.HouseId = houseId; ViewBag.Floor = floor;
        return View(rooms.Data ?? new());
    }

    public async Task<IActionResult> Details(int id)
    {
        var r = await _api.RoomAsync(id);
        if (!r.Success) return HandleFailure(r, "Index");
        await LoadAssetTypes();
        var siblings = await _api.RoomsAsync(r.Data!.HouseId);
        ViewBag.OtherRooms = new MultiSelectList((siblings.Data ?? new()).Where(x => x.Id != id), "Id", "RoomNumber");
        return View(r.Data);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int houseId, int floor = 1)
    {
        var house = await _api.HouseAsync(houseId);
        if (!house.Success) return HandleFailure(house, "Index");
        await LoadLookups(house.Data!.TotalFloors);
        return View(new CreateRoomDto { BoardingHouseId = houseId, Floor = floor, RoomNumber = (floor * 100 + 1).ToString() });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateRoomDto m)
    {
        var house = await _api.HouseAsync(m.BoardingHouseId);
        var totalFloors = house.Data?.TotalFloors ?? 50;
        if (m.Floor > totalFloors) ModelState.AddModelError(nameof(m.Floor), $"Nhà trọ chỉ có {totalFloors} tầng");
        if (!ModelState.IsValid) { await LoadLookups(totalFloors); return View(m); }
        var r = await _api.CreateRoomAsync(m);
        if (!r.Success) { r.AddToModelState(ModelState); await LoadLookups(totalFloors); return View(m); }
        TempData["Success"] = $"Đã tạo phòng {m.RoomNumber}.";
        return RedirectToAction("Details", "Houses", new { id = m.BoardingHouseId });
    }

    [HttpGet]
    public async Task<IActionResult> Bulk(int houseId, int floor = 1)
    {
        var house = await _api.HouseAsync(houseId);
        if (!house.Success) return HandleFailure(house, "Index");
        await LoadLookups(house.Data!.TotalFloors);
        return View(new BulkCreateRoomsDto { HouseId = houseId, TotalFloors = house.Data.TotalFloors, Floor = floor });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Bulk(BulkCreateRoomsDto m)
    {
        if (m.FromNumber > m.ToNumber) ModelState.AddModelError(nameof(m.ToNumber), "Số kết thúc phải lớn hơn hoặc bằng số bắt đầu");
        if (m.ToNumber - m.FromNumber + 1 > 30) ModelState.AddModelError(nameof(m.ToNumber), "Mỗi lần tạo tối đa 30 phòng");
        if (m.TotalFloors > 0 && m.Floor > m.TotalFloors) ModelState.AddModelError(nameof(m.Floor), $"Nhà trọ chỉ có {m.TotalFloors} tầng");
        if (!ModelState.IsValid) { await LoadLookups(m.TotalFloors); return View(m); }
        var r = await _api.BulkCreateAsync(m);
        if (!r.Success) { r.AddToModelState(ModelState); await LoadLookups(m.TotalFloors); return View(m); }
        TempData["Success"] = $"Đã tạo {r.Data!.Count} phòng: {string.Join(", ", r.Data.Select(x => x.RoomNumber))}.";
        return RedirectToAction("Details", "Houses", new { id = m.HouseId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var r = await _api.RoomAsync(id);
        if (!r.Success) return HandleFailure(r, "Index");
        var d = r.Data!;
        await LoadLookups(d.TotalFloors);
        return View(new UpdateRoomDto
        {
            Id = d.Id, BoardingHouseId = d.HouseId, TotalFloors = d.TotalFloors, RoomNumber = d.RoomNumber, Floor = d.Floor, Area = d.Area, Price = d.Price,
            DepositAmount = d.DepositAmount, Capacity = d.Capacity, Description = d.Description, AmenityIds = d.AmenityIds, ImageUrlsText = string.Join("\n", d.ImageUrls)
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UpdateRoomDto m)
    {
        if (m.TotalFloors > 0 && m.Floor > m.TotalFloors) ModelState.AddModelError(nameof(m.Floor), $"Nhà trọ chỉ có {m.TotalFloors} tầng");
        if (!ModelState.IsValid) { await LoadLookups(m.TotalFloors); return View(m); }
        var r = await _api.UpdateRoomAsync(m);
        if (!r.Success) { r.AddToModelState(ModelState); await LoadLookups(m.TotalFloors); return View(m); }
        TempData["Success"] = "Đã cập nhật phòng.";
        return RedirectToAction(nameof(Details), new { id = m.Id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int houseId)
    {
        Flash(await _api.DeleteRoomAsync(id), "Đã xóa phòng.");
        return RedirectToAction("Details", "Houses", new { id = houseId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetStatus(int id, string status)
    {
        Flash(await _api.SetRoomStatusAsync(id, status), "Đã đổi trạng thái phòng.");
        return RedirectToAction(nameof(Details), new { id });
    }

    // ----- Tài sản -----
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddAsset(RoomAssetDto m)
    {
        if (!ModelState.IsValid) { TempData["Error"] = "Thông tin tài sản chưa hợp lệ."; return RedirectToAction(nameof(Details), new { id = m.RoomId }); }
        Flash(await _api.AddAssetAsync(m), "Đã thêm tài sản.");
        return RedirectToAction(nameof(Details), new { id = m.RoomId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateAsset(RoomAssetDto m)
    {
        Flash(await _api.UpdateAssetAsync(m), "Đã cập nhật tài sản.");
        return RedirectToAction(nameof(Details), new { id = m.RoomId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAsset(int roomId, int assetId)
    {
        Flash(await _api.DeleteAssetAsync(roomId, assetId), "Đã xóa tài sản.");
        return RedirectToAction(nameof(Details), new { id = roomId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CopyAssets(CopyAssetsDto m)
    {
        if (m.TargetRoomIds.Count == 0) { TempData["Error"] = "Chọn ít nhất một phòng đích."; return RedirectToAction(nameof(Details), new { id = m.SourceRoomId }); }
        Flash(await _api.CopyAssetsAsync(m), $"Đã sao chép tài sản sang {m.TargetRoomIds.Count} phòng.");
        return RedirectToAction(nameof(Details), new { id = m.SourceRoomId });
    }

    private async Task LoadLookups(int totalFloors)
    {
        var l = await _public.LookupsAsync();
        ViewBag.Amenities = l.Data?.Amenities ?? new();
        ViewBag.AssetTypes = new SelectList(l.Data?.AssetTypes ?? new(), "Id", "Name");
        ViewBag.Floors = new SelectList(Enumerable.Range(1, Math.Max(1, totalFloors)).Select(f => new { Id = f, Name = $"Tầng {f}" }), "Id", "Name");
    }

    private async Task LoadAssetTypes()
    {
        var l = await _public.LookupsAsync();
        ViewBag.AssetTypes = new SelectList(l.Data?.AssetTypes ?? new(), "Id", "Name");
    }
}
