using System.ComponentModel.DataAnnotations;

namespace NestU.WebApp.Models.Billing;

public class UtilityReadingDto
{
    public int Id { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn hợp đồng")] [Display(Name = "Hợp đồng / phòng")] public int ContractId { get; set; }
    public string? RoomNumber { get; set; }
    public string? TenantName { get; set; }
    [Range(1, 12)] [Display(Name = "Tháng")] public int Month { get; set; } = DateTime.Today.Month;
    [Range(2020, 2100)] [Display(Name = "Năm")] public int Year { get; set; } = DateTime.Today.Year;
    [Range(0, int.MaxValue, ErrorMessage = "Chỉ số điện không hợp lệ")] [Display(Name = "Chỉ số điện (kWh)")] public int ElectricIndex { get; set; }
    [Range(0, int.MaxValue, ErrorMessage = "Chỉ số nước không hợp lệ")] [Display(Name = "Chỉ số nước (m³)")] public int WaterIndex { get; set; }
    public int? PreviousElectricIndex { get; set; }
    public int? PreviousWaterIndex { get; set; }
    public DateTime RecordedAt { get; set; }
    public bool HasInvoice { get; set; }
}

/// <summary>Giới hạn chỉ số cho một tháng (cùng tên với ReadingBoundsDto bên API).</summary>
public class ReadingBoundsDto
{
    public int ContractId { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public int PreviousElectricIndex { get; set; }
    public int PreviousWaterIndex { get; set; }
    public string PreviousLabel { get; set; } = string.Empty;
    public int? NextElectricIndex { get; set; }
    public int? NextWaterIndex { get; set; }
    public string? NextLabel { get; set; }
    public bool AlreadyRecorded { get; set; }
}

public class GenerateInvoiceDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn hợp đồng")] [Display(Name = "Hợp đồng / phòng")] public int ContractId { get; set; }
    [Range(1, 12)] [Display(Name = "Tháng")] public int Month { get; set; } = DateTime.Today.Month;
    [Range(2020, 2100)] [Display(Name = "Năm")] public int Year { get; set; } = DateTime.Today.Year;
    [DataType(DataType.Date)] [Display(Name = "Hạn thanh toán (để trống = ngày 10 tháng sau)")] public DateTime? DueDate { get; set; }
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
    public int InvoiceId { get; set; }
    [Range(1, 1_000_000_000, ErrorMessage = "Số tiền không hợp lệ")] [Display(Name = "Số tiền đã chuyển")] public decimal Amount { get; set; }
    [StringLength(500)] [Display(Name = "Ghi chú")] public string? Note { get; set; }
    [Display(Name = "Ảnh chuyển khoản")] public IFormFile? ProofImage { get; set; }
}

public class QrDto
{
    public string Content { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string? CheckoutUrl { get; set; }
}
