using System.ComponentModel.DataAnnotations;

namespace NestU.WebApp.Models.Assets;

public class AssetTemplateDto
{
    [Display(Name = "Loại")] public int AssetTypeId { get; set; }
    [StringLength(100)] [Display(Name = "Tên tài sản")] public string Name { get; set; } = string.Empty;
    [Range(1, 100)] [Display(Name = "Số lượng")] public int Quantity { get; set; } = 1;
    [Range(0, 1_000_000_000)] [Display(Name = "Giá trị ước tính")] public decimal EstimatedValue { get; set; }
}

public class RoomAssetDto
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn loại tài sản")] [Display(Name = "Loại tài sản")] public int AssetTypeId { get; set; }
    public string AssetTypeName { get; set; } = string.Empty;
    [Required(ErrorMessage = "Tên tài sản là bắt buộc")] [StringLength(100)] [Display(Name = "Tên tài sản")] public string Name { get; set; } = string.Empty;
    [Range(1, 100, ErrorMessage = "Số lượng tối thiểu 1")] [Display(Name = "Số lượng")] public int Quantity { get; set; } = 1;
    [Required] [Display(Name = "Tình trạng")] public string Condition { get; set; } = "Good";
    [Range(0, 1_000_000_000)] [Display(Name = "Giá trị ước tính (đ)")] public decimal EstimatedValue { get; set; }
    [StringLength(500)] [Display(Name = "Ghi chú")] public string? Note { get; set; }
}

public class CopyAssetsDto
{
    public int SourceRoomId { get; set; }
    [Display(Name = "Phòng đích")] public List<int> TargetRoomIds { get; set; } = new();
    [Display(Name = "Xóa tài sản cũ của phòng đích trước khi sao chép")] public bool ReplaceExisting { get; set; }
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
    public int RoomAssetId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int CheckInQuantity { get; set; }
    public string CheckInCondition { get; set; } = string.Empty;
    [Range(0, 100)] public int Quantity { get; set; }
    [Required] public string Condition { get; set; } = "Good";
    [Range(0, 1_000_000_000, ErrorMessage = "Tiền khấu trừ không hợp lệ")] public decimal DeductionAmount { get; set; }
    [StringLength(500)] public string? Note { get; set; }
}

public class CheckoutAssetsDto
{
    public int ContractId { get; set; }
    public List<CheckoutAssetItemDto> Items { get; set; } = new();
}
