using NestU.API.Entities.Enums;

namespace NestU.API.Entities;

public class Report
{
    public int Id { get; set; }
    public string ReporterId { get; set; } = string.Empty;
    public ApplicationUser Reporter { get; set; } = null!;
    public ReportTargetType TargetType { get; set; }
    public int TargetId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public ReportStatus Status { get; set; } = ReportStatus.Open;
    public string? ResolveNote { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
}
