using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.OData;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OData.ModelBuilder;
using Microsoft.OpenApi.Models;
using NestU.API.Data;
using NestU.API.Data.Seed;
using NestU.API.DTOs.Rooms;
using NestU.API.Entities;
using NestU.API.Helpers;
using NestU.API.Helpers.Authorization;
using NestU.API.Services;
using NestU.API.Services.Interfaces;

// Số thập phân dùng dấu chấm thống nhất giữa trình duyệt, Web App và API (tránh lỗi binding trên Windows tiếng Việt)
System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("en-US");
System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = System.Globalization.CultureInfo.GetCultureInfo("en-US");

var builder = WebApplication.CreateBuilder(args);

// ---------- CSDL ----------
builder.Services.AddDbContext<NestUDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ---------- Identity (mục 1.10) ----------
builder.Services.AddIdentityCore<ApplicationUser>(o =>
    {
        o.Password.RequiredLength = 6;
        o.Password.RequireNonAlphanumeric = false;
        o.Password.RequireUppercase = false;
        o.Password.RequireLowercase = false;
        o.Password.RequireDigit = false;
        o.User.RequireUniqueEmail = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<NestUDbContext>()
    .AddDefaultTokenProviders();

// ---------- JWT ----------
var jwt = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization(o =>
{
    o.AddPolicy(Policies.ApprovedLandlord, p => p.RequireRole(Roles.Landlord).AddRequirements(new ApprovedLandlordRequirement()));
});
builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, ApprovedLandlordHandler>();

// ---------- Services ----------
builder.Services.AddSingleton<JwtTokenGenerator>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IFileStorage, FileStorage>();
builder.Services.AddScoped<IHouseService, HouseService>();
builder.Services.AddScoped<IRoomService, RoomService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IRentalService, RentalService>();
builder.Services.AddScoped<IBillingService, BillingService>();
builder.Services.AddScoped<ISupportService, SupportService>();
builder.Services.AddScoped<IPublicService, PublicService>();
builder.Services.AddScoped<IStatsService, StatsService>();
builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();

// ---------- PayOS Client ----------
var payOsConfig = builder.Configuration.GetSection("PayOS");
if (!string.IsNullOrEmpty(payOsConfig["ClientId"]))
{
    builder.Services.AddSingleton(new PayOS.PayOSClient(
        payOsConfig["ClientId"]!,
        payOsConfig["ApiKey"]!,
        payOsConfig["ChecksumKey"]!
    ));
}

// ---------- Controllers + OData (mục 1.9) ----------
static Microsoft.OData.Edm.IEdmModel GetEdmModel()
{
    var odata = new ODataConventionModelBuilder();
    odata.EntitySet<RoomListDto>("Rooms").EntityType.HasKey(r => r.Id);
    return odata.GetEdmModel();
}

builder.Services.AddControllers(o => o.SuppressAsyncSuffixInActionNames = false)
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    })
    .AddOData(o => o
        .Select().Filter().OrderBy().Count().SetMaxTop(50).SkipToken()
        .AddRouteComponents("odata", GetEdmModel()));

// ---------- Lỗi → ProblemDetails ----------
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

// ---------- CORS (chỉ cần nếu sau này gọi API từ JS/SignalR) ----------
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins(builder.Configuration["Cors:WebAppOrigin"] ?? "https://localhost:7002")
    .AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

// ---------- Swagger có nút Authorize ----------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "NestU API", Version = "v1", Description = "Nền tảng quản lý nhà trọ — PRN232" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Dán JWT lấy từ POST /api/auth/login"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() }
    });
    // OData controller không có attribute route nên bỏ qua trong Swagger để tránh lỗi sinh tài liệu
    c.DocInclusionPredicate((_, api) => !string.IsNullOrEmpty(api.RelativePath) && !api.RelativePath.StartsWith("odata"));
    var xml = Path.Combine(AppContext.BaseDirectory, "NestU.API.xml");
    if (File.Exists(xml)) c.IncludeXmlComments(xml);
});

var app = builder.Build();

// ---------- Migrate + seed ----------
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    await DbSeeder.SeedAsync(scope.ServiceProvider, app.Configuration);
    await HistorySeeder.SeedAsync(scope.ServiceProvider, app.Configuration);
}

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "NestU API v1"));
}

app.UseHttpsRedirection();
app.UseStaticFiles();   // /uploads/... (ảnh phòng, giấy PCCC, ảnh chuyển khoản)
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
