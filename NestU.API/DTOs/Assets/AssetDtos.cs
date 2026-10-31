using System.ComponentModel.DataAnnotations;

namespace NestU.API.DTOs.Assets;

/// <summary>Mẫu tài sản dùng khi tạo phòng / tạo hàng loạt.</summary>
public class AssetTemplateDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn loại tài sản")] public int AssetTypeId { get; set; }
    [Required(ErrorMessage = "Tên tài sản là bắt buộc")] [StringLength(100)] public string Name { get; set; } = string.Empty;
    [Range(1, 100, ErrorMessage = "Số lượng tối thiểu 1")] public int Quantity { get; set; } = 1;
    [Range(0, 1_000_000_000)] public decimal EstimatedValue { get; set; }
}

public class RoomAssetDto
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn loại tài sản")] public int AssetTypeId { get; set; }
    public string AssetTypeName { get; set; } = string.Empty;
    [Required(ErrorMessage = "Tên tài sản là bắt buộc")] [StringLength(100)] public string Name { get; set; } = string.Empty;
    [Range(1, 100, ErrorMessage = "Số lượng tối thiểu 1")] public int Quantity { get; set; } = 1;
    /// <summary>Good, Damaged, Broken</summary>
    [Required] [RegularExpression("^(Good|Damaged|Broken)$", ErrorMessage = "Tình trạng không hợp lệ")] public string Condition { get; set; } = "Good";
    [Range(0, 1_000_000_000)] public decimal EstimatedValue { get; set; }
    [StringLength(500)] public string? Note { get; set; }
}

public class CopyAssetsDto
{
    [MinLength(1, ErrorMessage = "Chọn ít nhất một phòng đích")]
    public List<int> TargetRoomIds { get; set; } = new();
    public bool ReplaceExisting { get; set; }
}

public class ContractAssetDto
{
    public int Id { get; set; }
    public int ContractId { get; set; }
    public int RoomAssetId { get; set; }
    public string Stage { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string AssetTypeName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Condition { get; set; } = string.Empty;
    public decimal DeductionAmount { get; set; }
    public string? Note { get; set; }
    public DateTime? ConfirmedByTenantAt { get; set; }
}

public class CheckoutAssetItemDto
{
    [Range(1, int.MaxValue)] public int RoomAssetId { get; set; }
    [Range(0, 100)] public int Quantity { get; set; }
    [Required] [RegularExpression("^(Good|Damaged|Broken)$", ErrorMessage = "Tình trạng không hợp lệ")] public string Condition { get; set; } = "Good";
    [Range(0, 1_000_000_000, ErrorMessage = "Tiền khấu trừ không hợp lệ")] public decimal DeductionAmount { get; set; }
    [StringLength(500)] public string? Note { get; set; }
}

public class CheckoutAssetsDto
{
    [MinLength(1, ErrorMessage = "Danh sách tài sản trống")]
    public List<CheckoutAssetItemDto> Items { get; set; } = new();
}
