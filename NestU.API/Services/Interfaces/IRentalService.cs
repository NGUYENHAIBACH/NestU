using NestU.API.DTOs.Assets;
using NestU.API.DTOs.Common;
using NestU.API.DTOs.Rentals;

namespace NestU.API.Services.Interfaces;

public interface IRentalService
{
    // Hồ sơ Tenant
    Task<TenantProfileDto> GetTenantProfileAsync(string tenantId);
    Task UpdateTenantProfileAsync(string tenantId, TenantProfileDto dto);

    // Lịch xem
    Task<List<AppointmentDto>> GetTenantAppointmentsAsync(string tenantId);
    Task<AppointmentDto> CreateAppointmentAsync(string tenantId, CreateAppointmentDto dto);
    Task CancelAppointmentAsync(string tenantId, int id);
    Task<List<AppointmentDto>> GetLandlordAppointmentsAsync(string landlordId, string? status);
    Task DecideAppointmentAsync(string landlordId, int id, bool confirm, string? reason);

    // Yêu cầu thuê
    Task<List<RentalRequestDto>> GetTenantRequestsAsync(string tenantId);
    Task<RentalRequestDto> CreateRequestAsync(string tenantId, CreateRentalRequestDto dto);
    Task CancelRequestAsync(string tenantId, int id);
    Task<List<RentalRequestDto>> GetLandlordRequestsAsync(string landlordId, string? status);
    Task<ContractDto> ApproveRequestAsync(string landlordId, int id, ApproveRentalDto dto);
    Task RejectRequestAsync(string landlordId, int id, string reason);

    // Hợp đồng
    Task<List<ContractDto>> GetTenantContractsAsync(string tenantId);
    Task<ContractDto> GetContractAsync(string userId, string role, int id);
    Task<List<ContractDto>> GetLandlordContractsAsync(string landlordId, string? status, int? houseId);
    Task ExtendContractAsync(string landlordId, int id, ExtendContractDto dto);

    // Bàn giao tài sản
    Task<List<ContractAssetDto>> GetContractAssetsAsync(string userId, string role, int contractId, string? stage);
    Task ConfirmCheckInAsync(string tenantId, int contractId);
    Task CheckoutAssetsAsync(string landlordId, int contractId, CheckoutAssetsDto dto);

    // Gia hạn / trả phòng
    Task<ContractChangeRequestDto> CreateChangeRequestAsync(string tenantId, int contractId, string type, CreateContractChangeDto dto);
    Task<List<ContractChangeRequestDto>> GetChangeRequestsAsync(string userId, string role, string? status);
    Task DecideChangeRequestAsync(string landlordId, int id, DecisionDto dto);

    // Quyết toán cọc + chấm dứt
    Task<DepositSettlementDto> SettleDepositAsync(string landlordId, int contractId, CreateDepositSettlementDto dto);

    /// <summary>Ném 403/404; trả Contract đã Include Room.BoardingHouse.</summary>
    Task<Entities.Contract> GetOwnedContractAsync(string userId, string role, int contractId, bool track = false);
}
