using NestU.API.Entities.Enums;

namespace NestU.API.Entities;

public class ContractChangeRequest
{
    public int Id { get; set; }
    public int ContractId { get; set; }
    public Contract Contract { get; set; } = null!;
    public ChangeRequestType Type { get; set; }
    public DateTime? NewEndDate { get; set; }
    public string? Reason { get; set; }
    public ChangeRequestStatus Status { get; set; } = ChangeRequestStatus.Pending;
    public string? DecisionNote { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DecidedAt { get; set; }
}
