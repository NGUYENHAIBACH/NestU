using NestU.API.Entities.Enums;

namespace NestU.API.Entities;

public class Invoice
{
    public int Id { get; set; }
    public int ContractId { get; set; }
    public Contract Contract { get; set; } = null!;
    public int Month { get; set; }
    public int Year { get; set; }
    public decimal Total { get; set; }
    public DateTime DueDate { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Unpaid;
    public string? RejectReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<InvoiceDetail> Details { get; set; } = new List<InvoiceDetail>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();

    /// <summary>Trạng thái hiển thị: Unpaid quá hạn → "Overdue". Không lưu vào CSDL.</summary>
    public string DisplayStatus =>
        Status == InvoiceStatus.Unpaid && DueDate.Date < DateTime.Today ? "Overdue" : Status.ToString();
}
