using NestU.API.Entities.Enums;

namespace NestU.API.Entities;

public class RentalRequest
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public Room Room { get; set; } = null!;
    public string TenantId { get; set; } = string.Empty;
    public ApplicationUser Tenant { get; set; } = null!;
    public int NumberOfOccupants { get; set; } = 1;
    public DateTime MoveInDate { get; set; }
    public int DurationMonths { get; set; } = 6;
    public RentalRequestStatus Status { get; set; } = RentalRequestStatus.Pending;
    public string? Note { get; set; }
    public string? RejectReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
