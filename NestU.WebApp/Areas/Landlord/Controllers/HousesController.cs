using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using NestU.WebApp.ApiClients;
using NestU.WebApp.Controllers;
using NestU.WebApp.Models.Houses;

namespace NestU.WebApp.Areas.Landlord.Controllers;

[Area("Landlord")]
[Authorize(Roles = "Landlord")]
public class HousesController : BaseController
{
    private readonly LandlordApiClient _api;
    private readonly PublicApiClient _public;
    public HousesController(LandlordApiClient api, PublicApiClient @public) { _api = api; _public = @public; }

    public async Task<IActionResult> Index()
    {
        var r = await _api.HousesAsync();
        if (!r.Success) return HandleFailure(r, "Index");
        return View(r.Data);
    }

    public async Task<IActionResult> Details(int id)
    {
        var h = await _api.HouseAsync(id);
        if (!h.Success) return HandleFailure(h, "Index");
        var floors = await _api.FloorsAsync(id);
        var rooms = await _api.RoomsAsync(id);
        var certs = await _api.CertificatesAsync(id);
        ViewBag.Floors = floors.Data ?? new();
        ViewBag.Rooms = rooms.Data ?? new();
        ViewBag.Certificates = certs.Data ?? new();
        return View(h.Data);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadAreas(null);
        var dto = new CreateHouseDto();
        var houses = await _api.HousesAsync();
        var lastWithBank = houses.Data?.FirstOrDefault(h => !string.IsNullOrWhiteSpace(h.BankAccount));
        if (lastWithBank is not null)
        {
            dto.BankCode = lastWithBank.BankCode;
            dto.BankAccount = lastWithBank.BankAccount;
            dto.AccountName = lastWithBank.AccountName;
            ViewBag.PrefilledFromHouse = lastWithBank.Name;
        }
        else
        {
            var profile = await _api.ProfileAsync();
            if (!string.IsNullOrWhiteSpace(profile.Data?.FullName))
            {
                dto.AccountName = profile.Data.FullName.ToUpper();
            }
        }
        return View("Form", dto);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateHouseDto m)
    {
        if (!ModelState.IsValid) { await LoadAreas(m.AreaId); return View("Form", m); }
        var r = await _api.CreateHouseAsync(m);
        if (!r.Success) { r.AddToModelState(ModelState); await LoadAreas(m.AreaId); return View("Form", m); }
        TempData["Success"] = "Đã tạo nhà trọ. Hãy upload giấy PCCC để được duyệt hoạt động.";
        return RedirectToAction(nameof(Details), new { id = r.Data!.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var h = await _api.HouseAsync(id);
        if (!h.Success) return HandleFailure(h, "Index");
        var d = h.Data!;
        await LoadAreas(d.AreaId);
        return View("Form", new CreateHouseDto
        {
            Id = d.Id, Name = d.Name, Address = d.Address, AreaId = d.AreaId, Latitude = d.Latitude, Longitude = d.Longitude, TotalFloors = d.TotalFloors,
            ElectricityPrice = d.ElectricityPrice, WaterPrice = d.WaterPrice, WaterBillingType = d.WaterBillingType, AllowedTenantType = d.AllowedTenantType,
            Description = d.Description, BankCode = d.BankCode, BankAccount = d.BankAccount, AccountName = d.AccountName
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CreateHouseDto m)
    {
        m.Id = id;
        if (!ModelState.IsValid) { await LoadAreas(m.AreaId); return View("Form", m); }
        var r = await _api.UpdateHouseAsync(id, m);
        if (!r.Success) { r.AddToModelState(ModelState); await LoadAreas(m.AreaId); return View("Form", m); }
        TempData["Success"] = "Đã cập nhật nhà trọ.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        Flash(await _api.DeleteHouseAsync(id), "Đã xóa nhà trọ.");
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult UploadCertificate(int id) => View(new CreateCertificateDto { HouseId = id });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadCertificate(CreateCertificateDto m)
    {
        if (m.File is null) ModelState.AddModelError(nameof(m.File), "Vui lòng chọn file");
        if (!ModelState.IsValid) return View(m);
        var r = await _api.UploadCertificateAsync(m);
        if (!r.Success) { r.AddToModelState(ModelState); return View(m); }
        TempData["Success"] = "Đã gửi giấy PCCC, chờ quản trị viên duyệt.";
        return RedirectToAction(nameof(Details), new { id = m.HouseId });
    }

    private async Task LoadAreas(int? selected)
    {
        var l = await _public.LookupsAsync();
        ViewBag.Areas = new SelectList(l.Data?.Areas ?? new(), "Id", "Name", selected);
    }
}
