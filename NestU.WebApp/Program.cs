using Microsoft.AspNetCore.Authentication.Cookies;
using NestU.WebApp.ApiClients;
using NestU.WebApp.Handlers;

// Số thập phân dùng dấu chấm thống nhất giữa trình duyệt, Web App và API (tránh lỗi binding trên Windows tiếng Việt)
System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("en-US");
System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = System.Globalization.CultureInfo.GetCultureInfo("en-US");

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

// ---------- Cookie authentication mang claim role từ JWT (mục 2.7) ----------
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/Account/Login";
        o.LogoutPath = "/Account/Logout";
        o.AccessDeniedPath = "/Account/AccessDenied";
        o.ExpireTimeSpan = TimeSpan.FromMinutes(60);   // bằng thời hạn JWT
        o.SlidingExpiration = false;
        o.Cookie.Name = "NestU.Auth";
        o.Cookie.HttpOnly = true;
        o.Cookie.SameSite = SameSiteMode.Lax;
    });
builder.Services.AddAuthorization();

// ---------- Typed HttpClient gọi API (mục 2.6) ----------
builder.Services.AddTransient<JwtHandler>();
var apiBase = new Uri(builder.Configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7001/");

void AddApiClient<TClient>() where TClient : class =>
    builder.Services.AddHttpClient<TClient>(c =>
    {
        c.BaseAddress = apiBase;
        c.Timeout = TimeSpan.FromSeconds(30);
    })
    .AddHttpMessageHandler<JwtHandler>()
    // Dev: chấp nhận chứng chỉ localhost tự ký của API
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = builder.Environment.IsDevelopment()
            ? HttpClientHandler.DangerousAcceptAnyServerCertificateValidator : null
    });

AddApiClient<AuthApiClient>();
AddApiClient<PublicApiClient>();
AddApiClient<RoomApiClient>();
AddApiClient<LandlordApiClient>();
AddApiClient<TenantApiClient>();
AddApiClient<AdminApiClient>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error/500");
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/error/{0}");

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseMiddleware<VisitorCookieMiddleware>();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
