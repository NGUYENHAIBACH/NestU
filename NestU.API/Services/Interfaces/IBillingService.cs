using NestU.API.DTOs.Billing;

namespace NestU.API.Services.Interfaces;

public interface IBillingService
{
    Task<List<UtilityReadingDto>> GetReadingsAsync(string landlordId, int? contractId, int? houseId);
    Task<UtilityReadingDto> AddReadingAsync(string landlordId, UtilityReadingDto dto);
    Task<ReadingBoundsDto> GetReadingBoundsAsync(string landlordId, int contractId, int month, int year);

    Task<InvoiceDto> GenerateInvoiceAsync(string landlordId, GenerateInvoiceDto dto);
    Task<List<InvoiceDto>> GetLandlordInvoicesAsync(string landlordId, string? status, int? houseId);
    Task<List<InvoiceDto>> GetTenantInvoicesAsync(string tenantId);
    Task<InvoiceDto> GetInvoiceAsync(string userId, string role, int id);
    Task ReportPaymentAsync(string tenantId, int invoiceId, ReportPaymentDto dto);
    Task ConfirmPaymentAsync(string landlordId, int invoiceId);
    Task RejectPaymentAsync(string landlordId, int invoiceId, string reason);
    Task<QrDto> GetQrAsync(string tenantId, int invoiceId);
}
