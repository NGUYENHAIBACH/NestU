using NestU.API.Entities.Enums;

namespace NestU.API.Entities;

public class Contract
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public Room Room { get; set; } = null!;
    public string TenantId { get; set; } = string.Empty;
    public ApplicationUser Tenant { get; set; } = null!;
    public int? RentalRequestId { get; set; }
    public RentalRequest? RentalRequest { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal MonthlyRent { get; set; }
    public decimal DepositAmount { get; set; }
    public DateTime? DepositPaidAt { get; set; }
    public ContractStatus Status { get; set; } = ContractStatus.Active;
    public DateTime? TerminatedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ContractMember> Members { get; set; } = new List<ContractMember>();
    public ICollection<ContractAsset> Assets { get; set; } = new List<ContractAsset>();
    public ICollection<ContractService> Services { get; set; } = new List<ContractService>();
    public ICollection<UtilityReading> Readings { get; set; } = new List<UtilityReading>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    public DepositSettlement? DepositSettlement { get; set; }
}
