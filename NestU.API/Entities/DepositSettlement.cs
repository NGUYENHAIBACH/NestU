namespace NestU.API.Entities;

public class DepositSettlement
{
    public int Id { get; set; }
    public int ContractId { get; set; }
    public Contract Contract { get; set; } = null!;
    public decimal DepositAmount { get; set; }
    public decimal DamageDeduction { get; set; }
    public decimal UnpaidDeduction { get; set; }
    public decimal RefundAmount { get; set; }
    public string? Note { get; set; }
    public DateTime SettledAt { get; set; } = DateTime.UtcNow;
}
