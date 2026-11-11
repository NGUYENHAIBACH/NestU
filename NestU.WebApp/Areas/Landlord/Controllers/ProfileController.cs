using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NestU.WebApp.ApiClients;
using NestU.WebApp.Controllers;
using NestU.WebApp.Models.Auth;
using NestU.WebApp.Models.Houses;

namespace NestU.WebApp.Areas.Landlord.Controllers;

[Area("Landlord")]
[Authorize(Roles = "Landlord")]
public class ProfileController : BaseController
{
    private readonly LandlordApiClient _api;
    private readonly AuthApiClient _auth;
    public ProfileController(LandlordApiClient api, AuthApiClient auth) { _api = api; _auth = auth; }

    public async Task<IActionResult> Index()
    {
        var r = await _api.ProfileAsync();
        if (!r.Success) return HandleFailure(r, "Index");
        return View(r.Data);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(LandlordProfileDto m)
    {
        if (!ModelState.IsValid) return View(m);
        var r = await _api.UpdateProfileAsync(m);
        if (!r.Success) { r.AddToModelState(ModelState); return View(m); }
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
}
