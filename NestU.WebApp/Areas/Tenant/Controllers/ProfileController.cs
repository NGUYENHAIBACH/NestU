using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using NestU.WebApp.ApiClients;
using NestU.WebApp.Controllers;
using NestU.WebApp.Models.Auth;
using NestU.WebApp.Models.Rentals;

namespace NestU.WebApp.Areas.Tenant.Controllers;

[Area("Tenant")]
[Authorize(Roles = "Tenant")]
public class ProfileController : BaseController
{
    private readonly TenantApiClient _api;
    private readonly PublicApiClient _public;
    private readonly AuthApiClient _auth;
    public ProfileController(TenantApiClient api, PublicApiClient @public, AuthApiClient auth) { _api = api; _public = @public; _auth = auth; }

    public async Task<IActionResult> Index()
    {
        var r = await _api.ProfileAsync();
        if (!r.Success) return HandleFailure(r, "Index");
        await LoadUniversities(r.Data!.UniversityId);
        return View(r.Data);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(TenantProfileDto m)
    {
        if (m.IsStudent)
        {
            if (string.IsNullOrWhiteSpace(m.StudentCode))
                ModelState.AddModelError(nameof(m.StudentCode), "Sinh viên phải nhập mã số sinh viên");
            else if (!System.Text.RegularExpressions.Regex.IsMatch(m.StudentCode.Trim(), @"^[A-Z]{2}[0-9]{6}$"))
                ModelState.AddModelError(nameof(m.StudentCode), "Mã sinh viên phải gồm 2 ký tự in hoa không dấu và 6 chữ số (VD: SE123456)");

            if (string.IsNullOrWhiteSpace(m.UniversityName))
                ModelState.AddModelError(nameof(m.UniversityName), "Sinh viên phải chọn hoặc nhập tên trường");
        }
        else
        {
            m.StudentCode = null;
            m.UniversityName = null;
        }
        m.UniversityId = null; // gửi theo tên: API khớp trường có sẵn hoặc tạo trường mới
        if (!ModelState.IsValid) { await LoadUniversities(m.UniversityId); return View(m); }
        var r = await _api.UpdateProfileAsync(m);
        if (!r.Success)
        {
            if (r.Message?.Contains("mã sinh viên", StringComparison.OrdinalIgnoreCase) == true)
                ModelState.AddModelError(nameof(m.StudentCode), r.Message);
            r.AddToModelState(ModelState);
            await LoadUniversities(m.UniversityId);
            return View(m);
        }
        TempData["Success"] = "Đã cập nhật hồ sơ.";
        return RedirectToAction(nameof(Index));
    }

    // ----- Đổi mật khẩu (xác nhận bằng OTP gửi về email) -----
    [HttpGet]
    public async Task<IActionResult> ChangePassword()
    {
        await LoadEmail();
        ViewBag.OtpSent = TempData["OtpSent"] as bool? == true;
        return View(new ChangePasswordModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SendPasswordOtp()
    {
        var r = await _auth.SendChangePasswordOtpAsync();
        if (r.Success) TempData["OtpSent"] = true;
        else TempData["Error"] = r.Message ?? "Không gửi được mã OTP, vui lòng thử lại.";
        return RedirectToAction(nameof(ChangePassword));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordModel m)
    {
        if (ModelState.IsValid)
        {
            var r = await _auth.ChangePasswordAsync(m);
            if (r.Success)
            {
                TempData["Success"] = "Đã đổi mật khẩu. Lần đăng nhập sau hãy dùng mật khẩu mới.";
                return RedirectToAction(nameof(Index));
            }
            r.AddToModelState(ModelState);
        }
        await LoadEmail();
        ViewBag.OtpSent = true;
        return View(m);
    }

    private async Task LoadEmail() => ViewBag.Email = (await _api.ProfileAsync()).Data?.Email;

    private async Task LoadUniversities(int? selected)
    {
        var l = await _public.LookupsAsync();
        ViewBag.Universities = new SelectList(l.Data?.Universities ?? new(), "Id", "Name", selected);
    }
}
