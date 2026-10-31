using NestU.API.Entities.Enums;

namespace NestU.API.Entities;

public class MaintenanceRequest
{
    public int Id { get; set; }
    public int ContractId { get; set; }
    public Contract Contract { get; set; } = null!;
    public int? RoomAssetId { get; set; }
    public RoomAsset? RoomAsset { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public MaintenanceStatus Status { get; set; } = MaintenanceStatus.Open;
    public string? ResolveNote { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
}
