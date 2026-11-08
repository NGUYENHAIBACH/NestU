using System.ComponentModel.DataAnnotations;

namespace NestU.API.DTOs.Admin;

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

public class SetRoleDto
{
    [Required] [RegularExpression("^(Tenant|Landlord)$", ErrorMessage = "Chỉ gán được Tenant hoặc Landlord")]
    public string Role { get; set; } = "Tenant";
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

public class CreateReportDto
{
    /// <summary>Landlord, Room, House</summary>
    [Required] [RegularExpression("^(Landlord|Room|House)$", ErrorMessage = "Loại đối tượng không hợp lệ")]
    public string TargetType { get; set; } = "Room";
    [Range(1, int.MaxValue)] public int TargetId { get; set; }
    [Required(ErrorMessage = "Vui lòng nhập lý do")] [StringLength(1000, MinimumLength = 10, ErrorMessage = "Lý do từ 10 đến 1000 ký tự")]
    public string Reason { get; set; } = string.Empty;
}

public class AdminHouseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string LandlordName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsHidden { get; set; }
    public int RoomCount { get; set; }
}

public class SetHouseHiddenDto
{
    public bool IsHidden { get; set; }
}

public class ResolveReportDto
{
    /// <summary>Resolved hoặc Dismissed</summary>
    [Required] [RegularExpression("^(Resolved|Dismissed)$", ErrorMessage = "Kết quả không hợp lệ")]
    public string Status { get; set; } = "Resolved";
    /// <summary>Hành động kèm theo khi Resolved: None, HideHouse, LockLandlord</summary>
    [RegularExpression("^(None|HideHouse|LockLandlord)$")] public string Action { get; set; } = "None";
    [StringLength(500)] public string? Note { get; set; }
}

public class CatalogItemDto
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Tên là bắt buộc")] [StringLength(200)] public string Name { get; set; } = string.Empty;
    // University
    public string? ShortName { get; set; }
    [Range(-90, 90)] public double? Latitude { get; set; }
    [Range(-180, 180)] public double? Longitude { get; set; }
    // Amenity
    [StringLength(50)] public string? Icon { get; set; }
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
