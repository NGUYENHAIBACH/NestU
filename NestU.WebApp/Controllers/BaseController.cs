using Microsoft.AspNetCore.Mvc;
using NestU.WebApp.ApiClients;
using NestU.WebApp.Models.Common;

namespace NestU.WebApp.Controllers;

/// <summary>Tiện ích chung: đưa lỗi API vào TempData/ModelState, xử lý 401.</summary>
public abstract class BaseController : Controller
{
    protected IActionResult HandleFailure<T>(ApiResult<T> r, string? fallbackAction = null)
    {
        if (r.StatusCode == 401) return RedirectToAction("Login", "Account", new { area = "", returnUrl = Request.Path });
        if (r.StatusCode == 403) return RedirectToAction("AccessDenied", "Account", new { area = "" });
        return Fallback(r.StatusCode, r.Message, fallbackAction);
    }

    protected IActionResult HandleFailure(ApiResult r, string? fallbackAction = null)
    {
        if (r.StatusCode == 401) return RedirectToAction("Login", "Account", new { area = "", returnUrl = Request.Path });
        if (r.StatusCode == 403) return RedirectToAction("AccessDenied", "Account", new { area = "" });
        return Fallback(r.StatusCode, r.Message, fallbackAction);
    }

    /// <summary>Lỗi 5xx / API không chạy → về trang chủ (trang chủ chịu được API lỗi); lỗi khác → về action dự phòng,
    /// trừ khi action đó chính là action hiện tại (tránh vòng lặp redirect) thì cũng về trang chủ.</summary>
    private IActionResult Fallback(int status, string? message, string? fallbackAction)
    {
        TempData["Error"] = message ?? "Có lỗi xảy ra.";
        var current = ControllerContext.ActionDescriptor.ActionName;
        var target = fallbackAction ?? "Index";
        if (status >= 500 || status == 0 || string.Equals(current, target, StringComparison.OrdinalIgnoreCase))
            return RedirectToAction("Index", "Home", new { area = "" });
        return RedirectToAction(target);
    }

    protected void Flash(ApiResult r, string success)
    {
        if (r.Success) TempData["Success"] = success; else TempData["Error"] = r.Message ?? "Có lỗi xảy ra.";
    }

    protected void Flash<T>(ApiResult<T> r, string success)
    {
        if (r.Success) TempData["Success"] = success; else TempData["Error"] = r.Message ?? "Có lỗi xảy ra.";
    }
}
