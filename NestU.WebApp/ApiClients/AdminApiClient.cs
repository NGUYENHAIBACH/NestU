using NestU.WebApp.Models.Admin;
using NestU.WebApp.Models.Common;
using NestU.WebApp.Models.Houses;
using NestU.WebApp.Models.Support;

namespace NestU.WebApp.ApiClients;

public class AdminApiClient : ApiClientBase
{
    public AdminApiClient(HttpClient http) : base(http) { }

    public Task<ApiResult<PagedResult<UserListDto>>> UsersAsync(string? role, string? status, string? search, int page = 1, int pageSize = 20)
    {
        var q = new List<string> { $"page={page}", $"pageSize={pageSize}" };
        if (!string.IsNullOrEmpty(role)) q.Add($"role={role}");
        if (!string.IsNullOrEmpty(status)) q.Add($"status={status}");
        if (!string.IsNullOrWhiteSpace(search)) q.Add($"search={Uri.EscapeDataString(search)}");
        return GetAsync<PagedResult<UserListDto>>("api/admin/users?" + string.Join("&", q));
    }
    public Task<ApiResult> LockAsync(string id) => PutAsync($"api/admin/users/{id}/lock");
    public Task<ApiResult> UnlockAsync(string id) => PutAsync($"api/admin/users/{id}/unlock");
    public Task<ApiResult> SetRoleAsync(string id, string role) => PutAsync($"api/admin/users/{id}/role", new { Role = role });

    public Task<ApiResult<List<LandlordApprovalDto>>> LandlordApprovalsAsync(string? status) => GetAsync<List<LandlordApprovalDto>>("api/admin/landlord-approvals" + (string.IsNullOrEmpty(status) ? "" : $"?status={status}"));
    public Task<ApiResult> DecideLandlordAsync(DecisionModel m) => PutAsync($"api/admin/landlord-approvals/{m.Id}/decision", new { m.Approve, m.Note });

    public Task<ApiResult<List<CertificateDto>>> CertificatesAsync(string? status) => GetAsync<List<CertificateDto>>("api/admin/certificates" + (string.IsNullOrEmpty(status) ? "" : $"?status={status}"));
    public Task<ApiResult<CertificateDto>> CertificateAsync(int id) => GetAsync<CertificateDto>($"api/admin/certificates/{id}");
    public Task<ApiResult> ReviewCertificateAsync(DecisionModel m) => PutAsync($"api/admin/certificates/{m.Id}/review", new { m.Approve, m.Note });

    public Task<ApiResult<List<AdminHouseDto>>> HousesAsync() => GetAsync<List<AdminHouseDto>>("api/admin/houses");
    public Task<ApiResult> SetHouseHiddenAsync(int id, bool hidden) => PutAsync($"api/admin/houses/{id}/hidden", new SetHouseHiddenDto { IsHidden = hidden });

    public Task<ApiResult<List<ReportDto>>> ReportsAsync(string? status) => GetAsync<List<ReportDto>>("api/admin/reports" + (string.IsNullOrEmpty(status) ? "" : $"?status={status}"));
    public Task<ApiResult> ResolveReportAsync(ResolveReportDto m) => PutAsync($"api/admin/reports/{m.Id}/resolve", new { m.Status, m.Action, m.Note });

    public Task<ApiResult<List<CatalogItemDto>>> CatalogAsync(string type) => GetAsync<List<CatalogItemDto>>($"api/admin/catalog/{type}");
    public Task<ApiResult<CatalogItemDto>> CreateCatalogAsync(string type, CatalogItemDto m) => PostAsync<CatalogItemDto>($"api/admin/catalog/{type}", m);
    public Task<ApiResult> UpdateCatalogAsync(string type, CatalogItemDto m) => PutAsync($"api/admin/catalog/{type}/{m.Id}", m);
    public Task<ApiResult> DeleteCatalogAsync(string type, int id) => DeleteAsync($"api/admin/catalog/{type}/{id}");

    public Task<ApiResult<AdminStatsDto>> StatsAsync() => GetAsync<AdminStatsDto>("api/admin/stats");
}
