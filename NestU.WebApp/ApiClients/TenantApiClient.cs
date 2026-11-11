using System.Net.Http.Headers;
using NestU.WebApp.Models.Assets;
using NestU.WebApp.Models.Billing;
using NestU.WebApp.Models.Common;
using NestU.WebApp.Models.Rentals;
using NestU.WebApp.Models.Support;

namespace NestU.WebApp.ApiClients;

public class TenantApiClient : ApiClientBase
{
    public TenantApiClient(HttpClient http) : base(http) { }

    public Task<ApiResult<TenantProfileDto>> ProfileAsync() => GetAsync<TenantProfileDto>("api/tenant/profile");
    public Task<ApiResult> UpdateProfileAsync(TenantProfileDto m) => PutAsync("api/tenant/profile", m);

    public Task<ApiResult<List<AppointmentDto>>> AppointmentsAsync() => GetAsync<List<AppointmentDto>>("api/tenant/appointments");
    public Task<ApiResult<AppointmentDto>> CreateAppointmentAsync(CreateAppointmentDto m) => PostAsync<AppointmentDto>("api/tenant/appointments", new { m.RoomId, m.ScheduledAt, m.Note });
    public Task<ApiResult> CancelAppointmentAsync(int id) => PutAsync($"api/tenant/appointments/{id}/cancel");

    public Task<ApiResult<List<RentalRequestDto>>> RentalRequestsAsync() => GetAsync<List<RentalRequestDto>>("api/tenant/rental-requests");
    public Task<ApiResult<RentalRequestDto>> CreateRentalRequestAsync(CreateRentalRequestDto m) => PostAsync<RentalRequestDto>("api/tenant/rental-requests", new { m.RoomId, m.NumberOfOccupants, m.MoveInDate, m.DurationMonths, m.Note });
    public Task<ApiResult> CancelRentalRequestAsync(int id) => PutAsync($"api/tenant/rental-requests/{id}/cancel");

    public Task<ApiResult<List<ContractDto>>> ContractsAsync() => GetAsync<List<ContractDto>>("api/tenant/contracts");
    public Task<ApiResult<ContractDto>> ContractAsync(int id) => GetAsync<ContractDto>($"api/tenant/contracts/{id}");
    public Task<ApiResult<QrDto>> DepositQrAsync(int contractId) => GetAsync<QrDto>($"api/tenant/contracts/{contractId}/deposit-qr");
    public Task<ApiResult<List<ContractAssetDto>>> ContractAssetsAsync(int id, string? stage = null) => GetAsync<List<ContractAssetDto>>($"api/tenant/contracts/{id}/assets" + (stage is null ? "" : $"?stage={stage}"));
    public Task<ApiResult> ConfirmAssetsAsync(int id) => PutAsync($"api/tenant/contracts/{id}/assets/confirm");
    public Task<ApiResult<ContractChangeRequestDto>> CreateChangeAsync(CreateContractChangeDto m)
        => PostAsync<ContractChangeRequestDto>($"api/tenant/contracts/{m.ContractId}/{(m.Type == "Terminate" ? "termination-requests" : "renewal-requests")}", new { m.NewEndDate, m.Reason });
    public Task<ApiResult<List<ContractChangeRequestDto>>> ChangeRequestsAsync() => GetAsync<List<ContractChangeRequestDto>>("api/tenant/contract-requests");

    public Task<ApiResult<List<InvoiceDto>>> InvoicesAsync() => GetAsync<List<InvoiceDto>>("api/tenant/invoices");
    public Task<ApiResult<InvoiceDto>> InvoiceAsync(int id) => GetAsync<InvoiceDto>($"api/tenant/invoices/{id}");
    public Task<ApiResult<QrDto>> QrAsync(int id) => GetAsync<QrDto>($"api/tenant/invoices/{id}/qr");
    public Task<ApiResult> ReportPaymentAsync(ReportPaymentDto m)
    {
        var form = new MultipartFormDataContent
        {
            { new StringContent(m.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture)), "Amount" }
        };
        if (!string.IsNullOrEmpty(m.Note)) form.Add(new StringContent(m.Note), "Note");
        if (m.ProofImage is not null)
        {
            var sc = new StreamContent(m.ProofImage.OpenReadStream());
            sc.Headers.ContentType = new MediaTypeHeaderValue(m.ProofImage.ContentType);
            form.Add(sc, "ProofImage", m.ProofImage.FileName);
        }
        return PostFormAsync($"api/tenant/invoices/{m.InvoiceId}/payments", form);
    }

    public Task<ApiResult<List<MaintenanceDto>>> MaintenanceAsync() => GetAsync<List<MaintenanceDto>>("api/tenant/maintenance-requests");
    public Task<ApiResult<MaintenanceDto>> CreateMaintenanceAsync(CreateMaintenanceDto m)
    {
        var form = new MultipartFormDataContent
        {
            { new StringContent(m.ContractId.ToString()), "ContractId" },
            { new StringContent(m.Title), "Title" },
            { new StringContent(m.Description), "Description" }
        };
        if (m.RoomAssetId is not null) form.Add(new StringContent(m.RoomAssetId.Value.ToString()), "RoomAssetId");
        if (m.Image is not null)
        {
            var sc = new StreamContent(m.Image.OpenReadStream());
            sc.Headers.ContentType = new MediaTypeHeaderValue(m.Image.ContentType);
            form.Add(sc, "Image", m.Image.FileName);
        }
        return PostFormAsync<MaintenanceDto>("api/tenant/maintenance-requests", form);
    }

    public Task<ApiResult<List<ReviewDto>>> ReviewsAsync() => GetAsync<List<ReviewDto>>("api/tenant/reviews");
    public Task<ApiResult<ReviewDto>> CreateReviewAsync(CreateReviewDto m) => PostAsync<ReviewDto>("api/tenant/reviews", new { m.ContractId, m.Rating, m.Comment });
    public Task<ApiResult<List<ReportDto>>> ReportsAsync() => GetAsync<List<ReportDto>>("api/tenant/reports");
    public Task<ApiResult<ReportDto>> CreateReportAsync(CreateReportDto m) => PostAsync<ReportDto>("api/tenant/reports", new { m.TargetType, m.TargetId, m.Reason });
}
