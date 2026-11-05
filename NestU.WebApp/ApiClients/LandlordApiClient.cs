using System.Net.Http.Headers;
using NestU.WebApp.Models.Assets;
using NestU.WebApp.Models.Billing;
using NestU.WebApp.Models.Common;
using NestU.WebApp.Models.Houses;
using NestU.WebApp.Models.Rentals;
using NestU.WebApp.Models.Rooms;
using NestU.WebApp.Models.Support;

namespace NestU.WebApp.ApiClients;

public class LandlordApiClient : ApiClientBase
{
    public LandlordApiClient(HttpClient http) : base(http) { }

    // Hồ sơ
    public Task<ApiResult<LandlordProfileDto>> ProfileAsync() => GetAsync<LandlordProfileDto>("api/landlord/profile");
    public Task<ApiResult> UpdateProfileAsync(LandlordProfileDto m) => PutAsync("api/landlord/profile", m);

    // Nhà trọ
    public Task<ApiResult<List<HouseDto>>> HousesAsync() => GetAsync<List<HouseDto>>("api/landlord/houses");
    public Task<ApiResult<HouseDto>> HouseAsync(int id) => GetAsync<HouseDto>($"api/landlord/houses/{id}");
    public Task<ApiResult<HouseDto>> CreateHouseAsync(CreateHouseDto m) => PostAsync<HouseDto>("api/landlord/houses", m);
    public Task<ApiResult> UpdateHouseAsync(int id, CreateHouseDto m) => PutAsync($"api/landlord/houses/{id}", m);
    public Task<ApiResult> DeleteHouseAsync(int id) => DeleteAsync($"api/landlord/houses/{id}");
    public Task<ApiResult<List<FloorSummaryDto>>> FloorsAsync(int houseId) => GetAsync<List<FloorSummaryDto>>($"api/landlord/houses/{houseId}/floors");
    public Task<ApiResult<List<CertificateDto>>> CertificatesAsync(int houseId) => GetAsync<List<CertificateDto>>($"api/landlord/houses/{houseId}/certificates");

    public Task<ApiResult<CertificateDto>> UploadCertificateAsync(CreateCertificateDto m)
    {
        var form = new MultipartFormDataContent { { new StringContent(m.CertificateNo), "CertificateNo" } };
        if (m.File is not null)
        {
            var sc = new StreamContent(m.File.OpenReadStream());
            sc.Headers.ContentType = new MediaTypeHeaderValue(m.File.ContentType);
            form.Add(sc, "File", m.File.FileName);
        }
        return PostFormAsync<CertificateDto>($"api/landlord/houses/{m.HouseId}/certificates", form);
    }

    // Phòng
    public Task<ApiResult<List<RoomListDto>>> RoomsAsync(int? houseId = null, int? floor = null)
    {
        var q = new List<string>();
        if (houseId is not null) q.Add($"houseId={houseId}");
        if (floor is not null) q.Add($"floor={floor}");
        return GetAsync<List<RoomListDto>>("api/landlord/rooms" + (q.Count > 0 ? "?" + string.Join("&", q) : ""));
    }
    public Task<ApiResult<RoomDetailDto>> RoomAsync(int id) => GetAsync<RoomDetailDto>($"api/landlord/rooms/{id}");
    public Task<ApiResult<RoomDetailDto>> CreateRoomAsync(CreateRoomDto m) => PostAsync<RoomDetailDto>("api/landlord/rooms", new
    {
        m.BoardingHouseId, m.RoomNumber, m.Floor, m.Area, m.Price, m.DepositAmount, m.Capacity, m.Description, m.AmenityIds, m.ImageUrls,
        Assets = m.Assets.Where(a => a.AssetTypeId > 0 && !string.IsNullOrWhiteSpace(a.Name)).ToList()
    });
    public Task<ApiResult> UpdateRoomAsync(UpdateRoomDto m) => PutAsync($"api/landlord/rooms/{m.Id}", new
    {
        m.RoomNumber, m.Floor, m.Area, m.Price, m.DepositAmount, m.Capacity, m.Description, m.AmenityIds, m.ImageUrls
    });
    public Task<ApiResult> DeleteRoomAsync(int id) => DeleteAsync($"api/landlord/rooms/{id}");
    public Task<ApiResult> SetRoomStatusAsync(int id, string status) => PutAsync($"api/landlord/rooms/{id}/status", new { Status = status });
    public Task<ApiResult<List<RoomListDto>>> BulkCreateAsync(BulkCreateRoomsDto m) => PostAsync<List<RoomListDto>>($"api/landlord/houses/{m.HouseId}/rooms/bulk", new
    {
        m.Floor, m.FromNumber, m.ToNumber, m.Area, m.Price, m.DepositAmount, m.Capacity, m.Description, m.AmenityIds,
        Assets = m.Assets.Where(a => a.AssetTypeId > 0 && !string.IsNullOrWhiteSpace(a.Name)).ToList()
    });

    // Tài sản
    public Task<ApiResult<List<RoomAssetDto>>> AssetsAsync(int roomId) => GetAsync<List<RoomAssetDto>>($"api/landlord/rooms/{roomId}/assets");
    public Task<ApiResult<RoomAssetDto>> AddAssetAsync(RoomAssetDto m) => PostAsync<RoomAssetDto>($"api/landlord/rooms/{m.RoomId}/assets", m);
    public Task<ApiResult> UpdateAssetAsync(RoomAssetDto m) => PutAsync($"api/landlord/rooms/{m.RoomId}/assets/{m.Id}", m);
    public Task<ApiResult> DeleteAssetAsync(int roomId, int assetId) => DeleteAsync($"api/landlord/rooms/{roomId}/assets/{assetId}");
    public Task<ApiResult> CopyAssetsAsync(CopyAssetsDto m) => PostAsync($"api/landlord/rooms/{m.SourceRoomId}/assets/copy", new { m.TargetRoomIds, m.ReplaceExisting });

    // Dịch vụ
    public Task<ApiResult<List<ServiceDto>>> ServicesAsync(int? houseId = null) => GetAsync<List<ServiceDto>>("api/landlord/services" + (houseId is null ? "" : $"?houseId={houseId}"));
    public Task<ApiResult<ServiceDto>> CreateServiceAsync(ServiceDto m) => PostAsync<ServiceDto>("api/landlord/services", m);
    public Task<ApiResult> UpdateServiceAsync(ServiceDto m) => PutAsync($"api/landlord/services/{m.Id}", m);
    public Task<ApiResult> DeleteServiceAsync(int id) => DeleteAsync($"api/landlord/services/{id}");

    // Lịch xem, yêu cầu thuê
    public Task<ApiResult<List<AppointmentDto>>> AppointmentsAsync(string? status = null) => GetAsync<List<AppointmentDto>>("api/landlord/appointments" + Q(status));
    public Task<ApiResult> ConfirmAppointmentAsync(int id) => PutAsync($"api/landlord/appointments/{id}/confirm");
    public Task<ApiResult> RejectAppointmentAsync(int id, string reason) => PutAsync($"api/landlord/appointments/{id}/reject", new { Reason = reason });
    public Task<ApiResult<List<RentalRequestDto>>> RentalRequestsAsync(string? status = null) => GetAsync<List<RentalRequestDto>>("api/landlord/rental-requests" + Q(status));
    public Task<ApiResult<ContractDto>> ApproveRequestAsync(ApproveRentalDto m) => PutAsync<ContractDto>($"api/landlord/rental-requests/{m.RequestId}/approve", new
    {
        m.StartDate, m.DurationMonths, m.DepositAmount, m.DepositPaid, m.ServiceIds,
        Members = m.Members.Where(x => !string.IsNullOrWhiteSpace(x.FullName)).ToList()
    });
    public Task<ApiResult> RejectRequestAsync(int id, string reason) => PutAsync($"api/landlord/rental-requests/{id}/reject", new { Reason = reason });

    // Hợp đồng
    public Task<ApiResult<List<ContractDto>>> ContractsAsync(string? status = null, int? houseId = null)
    {
        var q = new List<string>();
        if (!string.IsNullOrEmpty(status)) q.Add($"status={status}");
        if (houseId is not null) q.Add($"houseId={houseId}");
        return GetAsync<List<ContractDto>>("api/landlord/contracts" + (q.Count > 0 ? "?" + string.Join("&", q) : ""));
    }
    public Task<ApiResult<ContractDto>> ContractAsync(int id) => GetAsync<ContractDto>($"api/landlord/contracts/{id}");
    public Task<ApiResult> ExtendAsync(ExtendContractDto m) => PutAsync($"api/landlord/contracts/{m.Id}/extend", new { m.NewEndDate });
    public Task<ApiResult<List<ContractAssetDto>>> ContractAssetsAsync(int contractId, string? stage = null) => GetAsync<List<ContractAssetDto>>($"api/landlord/contracts/{contractId}/assets" + Q(stage, "stage"));
    public Task<ApiResult> CheckoutAsync(CheckoutAssetsDto m) => PutAsync($"api/landlord/contracts/{m.ContractId}/checkout-assets", new
    {
        Items = m.Items.Select(i => new { i.RoomAssetId, i.Quantity, i.Condition, i.DeductionAmount, i.Note }).ToList()
    });
    public Task<ApiResult<DepositSettlementDto>> SettleAsync(CreateDepositSettlementDto m) => PostAsync<DepositSettlementDto>($"api/landlord/contracts/{m.ContractId}/deposit-settlement", new { m.Note, m.DeductUnpaidInvoices });
    public Task<ApiResult<List<ContractChangeRequestDto>>> ChangeRequestsAsync(string? status = null) => GetAsync<List<ContractChangeRequestDto>>("api/landlord/contract-requests" + Q(status));
    public Task<ApiResult> DecideChangeAsync(DecisionModel m) => PutAsync($"api/landlord/contract-requests/{m.Id}/decision", new { m.Approve, m.Note });

    // Điện nước, hóa đơn
    public Task<ApiResult<List<UtilityReadingDto>>> ReadingsAsync(int? houseId = null) => GetAsync<List<UtilityReadingDto>>("api/landlord/readings" + (houseId is null ? "" : $"?houseId={houseId}"));
    public Task<ApiResult<UtilityReadingDto>> AddReadingAsync(UtilityReadingDto m) => PostAsync<UtilityReadingDto>("api/landlord/readings", new { m.ContractId, m.Month, m.Year, m.ElectricIndex, m.WaterIndex });
    public Task<ApiResult<List<InvoiceDto>>> InvoicesAsync(string? status = null, int? houseId = null)
    {
        var q = new List<string>();
        if (!string.IsNullOrEmpty(status)) q.Add($"status={status}");
        if (houseId is not null) q.Add($"houseId={houseId}");
        return GetAsync<List<InvoiceDto>>("api/landlord/invoices" + (q.Count > 0 ? "?" + string.Join("&", q) : ""));
    }
    public Task<ApiResult<InvoiceDto>> InvoiceAsync(int id) => GetAsync<InvoiceDto>($"api/landlord/invoices/{id}");
    public Task<ApiResult<InvoiceDto>> GenerateInvoiceAsync(GenerateInvoiceDto m) => PostAsync<InvoiceDto>("api/landlord/invoices/generate", m);
    public Task<ApiResult> ConfirmPaymentAsync(int id) => PutAsync($"api/landlord/invoices/{id}/confirm-payment");
    public Task<ApiResult> RejectPaymentAsync(int id, string reason) => PutAsync($"api/landlord/invoices/{id}/reject-payment", new { Reason = reason });

    // Sự cố
    public Task<ApiResult<List<MaintenanceDto>>> MaintenanceAsync(string? status = null) => GetAsync<List<MaintenanceDto>>("api/landlord/maintenance-requests" + Q(status));
    public Task<ApiResult> UpdateMaintenanceAsync(UpdateMaintenanceStatusDto m) => PutAsync($"api/landlord/maintenance-requests/{m.Id}/status", new { m.Status, m.Note, m.AssetCondition });

    private static string Q(string? v, string name = "status") => string.IsNullOrEmpty(v) ? "" : $"?{name}={Uri.EscapeDataString(v)}";
}
