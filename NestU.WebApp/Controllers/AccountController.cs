using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using NestU.WebApp.ApiClients;
using NestU.WebApp.Models.Auth;

namespace NestU.WebApp.Controllers;

public class AccountController : Controller
{
    private readonly AuthApiClient _auth;
    private readonly PublicApiClient _public;

    public AccountController(AuthApiClient auth, PublicApiClient @public)
    {
        _auth = auth;
        _public = @public;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToDashboard(User.FindFirstValue(ClaimTypes.Role));
        return View(new LoginModel { ReturnUrl = returnUrl });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _auth.LoginAsync(model);
        if (!result.Success || result.Data is null)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "Đăng nhập thất bại.");
            return View(model);
        }

        var t = result.Data;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, t.UserId),
            new(ClaimTypes.Name, t.FullName),
            new(ClaimTypes.Email, t.Email),
            new(ClaimTypes.Role, t.Role),
            new("access_token", t.AccessToken)
        };
        if (t.LandlordApprovalStatus is not null) claims.Add(new Claim("landlord_status", t.LandlordApprovalStatus));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = false, ExpiresUtc = t.ExpiresAt });

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl)) return Redirect(model.ReturnUrl);
        return RedirectToDashboard(t.Role);
    }

    [HttpGet]
    public async Task<IActionResult> Register()
    {
        await LoadUniversities();
        return View(new RegisterModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterModel model)
    {
        if (model.Role != "Tenant") { model.IsStudent = false; model.StudentCode = null; model.UniversityId = null; }
        if (!ModelState.IsValid) { await LoadUniversities(); return View(model); }

        var result = await _auth.RegisterAsync(model);
        if (!result.Success)
        {
            result.AddToModelState(ModelState);
            await LoadUniversities();
            return View(model);
        }

        TempData["Success"] = model.Role == "Landlord"
            ? "Đăng ký thành công. Hồ sơ chủ trọ của bạn đang chờ quản trị viên duyệt."
            : "Đăng ký thành công. Hãy đăng nhập để bắt đầu tìm phòng.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult ForgotPassword() => View(new ForgotPasswordModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _auth.ForgotPasswordAsync(model);
        if (!result.Success)
        {
            result.AddToModelState(ModelState);
            return View(model);
        }

        TempData["Success"] = "Mã xác thực OTP đã được gửi đến email của bạn (hiệu lực trong 5 phút).";
        return RedirectToAction(nameof(ResetPassword), new { email = model.Email });
    }

    [HttpGet]
    public IActionResult ResetPassword(string? email)
    {
        return View(new ResetPasswordModel { Email = email ?? string.Empty });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _auth.ResetPasswordAsync(model);
        if (!result.Success)
        {
            result.AddToModelState(ModelState);
            return View(model);
        }

        TempData["Success"] = "Đặt lại mật khẩu thành công! Vui lòng đăng nhập bằng mật khẩu mới.";
        return RedirectToAction(nameof(Login));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();

    private IActionResult RedirectToDashboard(string? role) => role switch
    {
        "Admin" => RedirectToAction("Index", "Dashboard", new { area = "Admin" }),
        "Landlord" => RedirectToAction("Index", "Dashboard", new { area = "Landlord" }),
        "Tenant" => RedirectToAction("Index", "Dashboard", new { area = "Tenant" }),
        _ => RedirectToAction("Index", "Home")
    };

    private async Task LoadUniversities()
    {
        var lookups = await _public.LookupsAsync();
        ViewBag.Universities = new SelectList(lookups.Data?.Universities ?? new(), "Id", "Name");
    }
}
