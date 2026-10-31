using NestU.API.Entities.Enums;

namespace NestU.API.Entities;

public class ViewingAppointment
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public Room Room { get; set; } = null!;
    public string TenantId { get; set; } = string.Empty;
    public ApplicationUser Tenant { get; set; } = null!;
    public DateTime ScheduledAt { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;
    public string? Note { get; set; }
    public string? RejectReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
