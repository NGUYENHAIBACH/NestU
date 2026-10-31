using System.ComponentModel.DataAnnotations;

namespace NestU.API.DTOs.Support;

public class CreateMaintenanceDto
{
    [Range(1, int.MaxValue)] public int ContractId { get; set; }
    public int? RoomAssetId { get; set; }
    [Required(ErrorMessage = "Tiêu đề là bắt buộc")] [StringLength(200)] public string Title { get; set; } = string.Empty;
    [Required(ErrorMessage = "Mô tả là bắt buộc")] [StringLength(2000)] public string Description { get; set; } = string.Empty;
    public IFormFile? Image { get; set; }
}

public class MaintenanceDto
{
    public int Id { get; set; }
    public int ContractId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public string HouseName { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
    public int? RoomAssetId { get; set; }
    public string? AssetName { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ResolveNote { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}

public class UpdateMaintenanceStatusDto
{
    /// <summary>InProgress hoặc Resolved</summary>
    [Required] [RegularExpression("^(InProgress|Resolved)$", ErrorMessage = "Trạng thái không hợp lệ")] public string Status { get; set; } = "InProgress";
    [StringLength(500)] public string? Note { get; set; }
    /// <summary>Khi Resolved có gắn tài sản: cập nhật tình trạng tài sản (Good/Damaged/Broken).</summary>
    [RegularExpression("^(Good|Damaged|Broken)$")] public string? AssetCondition { get; set; }
}

public class CreateReviewDto
{
    [Range(1, int.MaxValue)] public int ContractId { get; set; }
    [Range(1, 5, ErrorMessage = "Điểm đánh giá từ 1 đến 5")] public int Rating { get; set; }
    [StringLength(1000)] public string? Comment { get; set; }
}

public class ReviewDto
{
    public int Id { get; set; }
    public int ContractId { get; set; }
    public int RoomId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}
