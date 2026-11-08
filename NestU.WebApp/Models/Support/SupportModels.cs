using System.ComponentModel.DataAnnotations;

namespace NestU.WebApp.Models.Support;

public class CreateMaintenanceDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn hợp đồng")] [Display(Name = "Hợp đồng")] public int ContractId { get; set; }
    [Display(Name = "Tài sản liên quan (nếu có)")] public int? RoomAssetId { get; set; }
    [Required(ErrorMessage = "Tiêu đề là bắt buộc")] [StringLength(200)] [Display(Name = "Tiêu đề")] public string Title { get; set; } = string.Empty;
    [Required(ErrorMessage = "Mô tả là bắt buộc")] [StringLength(2000)] [Display(Name = "Mô tả sự cố")] public string Description { get; set; } = string.Empty;
    [Display(Name = "Ảnh (tuỳ chọn)")] public IFormFile? Image { get; set; }
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
    public int Id { get; set; }
    public bool HasAsset { get; set; }
    [Required] [Display(Name = "Trạng thái")] public string Status { get; set; } = "InProgress";
    [StringLength(500)] [Display(Name = "Ghi chú xử lý")] public string? Note { get; set; }
    [Display(Name = "Tình trạng tài sản sau sửa")] public string? AssetCondition { get; set; }
}

public class CreateReviewDto
{
    [Range(1, int.MaxValue)] public int ContractId { get; set; }
    public string? RoomLabel { get; set; }
    [Range(1, 5, ErrorMessage = "Điểm từ 1 đến 5")] [Display(Name = "Điểm đánh giá")] public int Rating { get; set; } = 5;
    [StringLength(1000)] [Display(Name = "Nhận xét")] public string? Comment { get; set; }
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

public class CreateReportDto
{
    [Required] [RegularExpression("^(Landlord|Room|House)$", ErrorMessage = "Loại đối tượng không hợp lệ")] [Display(Name = "Đối tượng")] public string TargetType { get; set; } = "Room";
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn đối tượng")] public int TargetId { get; set; }
    public string? TargetLabel { get; set; }
    [Required(ErrorMessage = "Vui lòng nhập lý do")] [StringLength(1000, MinimumLength = 10, ErrorMessage = "Lý do từ 10 đến 1000 ký tự")] [Display(Name = "Nội dung khiếu nại")]
    public string Reason { get; set; } = string.Empty;
}

public class ReportDto
{
    public int Id { get; set; }
    public string ReporterId { get; set; } = string.Empty;
    public string ReporterName { get; set; } = string.Empty;
    public string TargetType { get; set; } = string.Empty;
    public int TargetId { get; set; }
    public string TargetName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ResolveNote { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}
