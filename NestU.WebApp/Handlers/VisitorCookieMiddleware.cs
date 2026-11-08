namespace NestU.WebApp.Handlers;

/// <summary>Cấp cookie nestu_visitor (GUID) cho mỗi trình duyệt; JwtHandler gửi nó qua header X-Visitor-Id để API đếm ViewCount.</summary>
public class VisitorCookieMiddleware
{
    public const string CookieName = "nestu_visitor";
    public const string ItemKey = "VisitorId";
    private readonly RequestDelegate _next;

    public VisitorCookieMiddleware(RequestDelegate next) => _next = next;

    public Task InvokeAsync(HttpContext context)
    {
        var current = context.Request.Cookies[CookieName];
        if (!Guid.TryParse(current, out _))
        {
            current = Guid.NewGuid().ToString();
            context.Response.Cookies.Append(CookieName, current, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                IsEssential = true,
                MaxAge = TimeSpan.FromDays(365)
            });
        }
        context.Items[ItemKey] = current;   // lần đầu cookie chưa có trong Request nên truyền qua Items
        return _next(context);
    }
}
