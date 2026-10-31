using System.ComponentModel.DataAnnotations;

namespace NestU.WebApp.Models.Admin;

public class UserListDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool IsLocked { get; set; }
    public string? LandlordApprovalStatus { get; set; }
    public bool? IsStudent { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class LandlordApprovalDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string CitizenId { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string ApprovalStatus { get; set; } = string.Empty;
    public string? ReviewNote { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
}

public class ResolveReportDto
{
    public int Id { get; set; }
    [Required] [Display(Name = "Kết quả")] public string Status { get; set; } = "Resolved";
    [Display(Name = "Hành động kèm theo")] public string Action { get; set; } = "None";
    [StringLength(500)] [Display(Name = "Ghi chú")] public string? Note { get; set; }
}

public class CatalogItemDto
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Tên là bắt buộc")] [StringLength(200)] [Display(Name = "Tên")] public string Name { get; set; } = string.Empty;
    [Display(Name = "Tên viết tắt")] public string? ShortName { get; set; }
    [Range(-90, 90)] [Display(Name = "Vĩ độ")] public double? Latitude { get; set; }
    [Range(-180, 180)] [Display(Name = "Kinh độ")] public double? Longitude { get; set; }
    [StringLength(50)] [Display(Name = "Icon (bootstrap-icons)")] public string? Icon { get; set; }
}

public class AdminStatsDto
{
    public int TotalUsers { get; set; }
    public int PendingLandlords { get; set; }
    public int PendingCertificates { get; set; }
    public int ActiveHouses { get; set; }
    public int TotalRooms { get; set; }
    public int ActiveContracts { get; set; }
    public int OpenReports { get; set; }
}
