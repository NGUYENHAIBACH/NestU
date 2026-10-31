using NestU.API.Entities.Enums;

namespace NestU.API.Entities;

public class ContractAsset
{
    public int Id { get; set; }
    public int ContractId { get; set; }
    public Contract Contract { get; set; } = null!;
    public int RoomAssetId { get; set; }
    public RoomAsset RoomAsset { get; set; } = null!;
    public AssetStage Stage { get; set; }
    public string Name { get; set; } = string.Empty;   // chụp lại tên lúc bàn giao
    public int Quantity { get; set; }
    public AssetCondition Condition { get; set; }
    public decimal DeductionAmount { get; set; }
    public string? Note { get; set; }
    public DateTime? ConfirmedByTenantAt { get; set; }
}
