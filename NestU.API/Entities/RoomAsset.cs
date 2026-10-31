using NestU.API.Entities.Enums;

namespace NestU.API.Entities;

public class RoomAsset
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public Room Room { get; set; } = null!;
    public int AssetTypeId { get; set; }
    public AssetType AssetType { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public AssetCondition Condition { get; set; } = AssetCondition.Good;
    public decimal EstimatedValue { get; set; }
    public string? Note { get; set; }
}
