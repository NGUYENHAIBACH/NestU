using System.ComponentModel.DataAnnotations;

namespace NestU.API.DTOs.Billing;

public class UtilityReadingDto
{
    public int Id { get; set; }
    [Range(1, int.MaxValue)] public int ContractId { get; set; }
    public string? RoomNumber { get; set; }
    public string? TenantName { get; set; }
    [Range(1, 12, ErrorMessage = "Tháng từ 1 đến 12")] public int Month { get; set; }
    [Range(2020, 2100)] public int Year { get; set; }
    [Range(0, int.MaxValue, ErrorMessage = "Chỉ số điện không hợp lệ")] public int ElectricIndex { get; set; }
    [Range(0, int.MaxValue, ErrorMessage = "Chỉ số nước không hợp lệ")] public int WaterIndex { get; set; }
    public int? PreviousElectricIndex { get; set; }
    public int? PreviousWaterIndex { get; set; }
    public DateTime RecordedAt { get; set; }
    public bool HasInvoice { get; set; }
}

public class GenerateInvoiceDto
{
    [Range(1, int.MaxValue)] public int ContractId { get; set; }
    [Range(1, 12, ErrorMessage = "Tháng từ 1 đến 12")] public int Month { get; set; }
    [Range(2020, 2100)] public int Year { get; set; }
    /// <summary>Hạn thanh toán; null → ngày 10 tháng sau.</summary>
    public DateTime? DueDate { get; set; }
}

public class InvoiceDetailDto
{
    public string ItemName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string? Unit { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
}

public class PaymentDto
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public string? ProofImageUrl { get; set; }
    public string? Note { get; set; }
    public DateTime ReportedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
}

public class InvoiceDto
{
    public int Id { get; set; }
    public int ContractId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public string HouseName { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
    public int Month { get; set; }
    public int Year { get; set; }
    public decimal Total { get; set; }
    public decimal RemainingAmount { get; set; }   // còn phải trả (Total trừ khoản đã xác nhận, vd phần trừ vào cọc)
    public DateTime DueDate { get; set; }
    public string Status { get; set; } = string.Empty;
    /// <summary>Unpaid quá hạn → "Overdue" (tính khi đọc).</summary>
    public string DisplayStatus { get; set; } = string.Empty;
    public string? RejectReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<InvoiceDetailDto> Details { get; set; } = new();
    public List<PaymentDto> Payments { get; set; } = new();
    public string? BankCode { get; set; }
    public string? BankAccount { get; set; }
    public string? AccountName { get; set; }
}

public class ReportPaymentDto
{
    [Range(1, 1_000_000_000, ErrorMessage = "Số tiền không hợp lệ")] public decimal Amount { get; set; }
    [StringLength(500)] public string? Note { get; set; }
    public IFormFile? ProofImage { get; set; }
}

public class QrDto
{
    public string Content { get; set; } = string.Empty;   // chuỗi VietQR hoặc URL ảnh QR
    public string ImageUrl { get; set; } = string.Empty;
}
