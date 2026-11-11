using System.ComponentModel.DataAnnotations;
using NestU.API.DTOs.Rooms;

namespace NestU.API.DTOs.Houses;

public class CreateHouseDto
{
    [Required(ErrorMessage = "Tên nhà trọ là bắt buộc")] [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Địa chỉ là bắt buộc")] [StringLength(300)]
    public string Address { get; set; } = string.Empty;

    /// <summary>Khu vực chủ trọ tự chọn; để trống thì suy ra từ <see cref="DistrictName"/>.</summary>
    [Range(1, int.MaxValue, ErrorMessage = "Khu vực không hợp lệ")]
    public int? AreaId { get; set; }

    /// <summary>Tên quận/huyện từ bộ chọn địa chỉ, ví dụ "Quận Bình Thạnh". Dùng khi không chọn khu vực.</summary>
    [StringLength(100)]
    public string? DistrictName { get; set; }

    [Range(-90, 90)] public double? Latitude { get; set; }
    [Range(-180, 180)] public double? Longitude { get; set; }

    [Range(1, 50, ErrorMessage = "Số tầng từ 1 đến 50")]
    public int TotalFloors { get; set; } = 1;

    // Quy định 2
    [Range(1, 4000, ErrorMessage = "Giá điện tối đa 4.000đ/kWh")]
    public decimal ElectricityPrice { get; set; }

    [Range(1, 1_000_000, ErrorMessage = "Giá nước phải lớn hơn 0")]
    public decimal WaterPrice { get; set; }

    /// <summary>PerM3 hoặc PerPerson</summary>
    [Required] [RegularExpression("^(PerM3|PerPerson)$", ErrorMessage = "Cách tính nước không hợp lệ")]
    public string WaterBillingType { get; set; } = "PerM3";

    /// <summary>All hoặc StudentOnly</summary>
    [Required] [RegularExpression("^(All|StudentOnly)$", ErrorMessage = "Đối tượng thuê không hợp lệ")]
    public string AllowedTenantType { get; set; } = "All";

    [StringLength(2000)] public string? Description { get; set; }
    [StringLength(20)] public string? BankCode { get; set; }
    [StringLength(30)] public string? BankAccount { get; set; }
    [StringLength(100)] public string? AccountName { get; set; }
}

public class UpdateHouseDto : CreateHouseDto { }

public class HouseDto
{
    public int Id { get; set; }
    public bool IsHidden { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public int AreaId { get; set; }
    public string AreaName { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public int TotalFloors { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal ElectricityPrice { get; set; }
    public decimal WaterPrice { get; set; }
    public string WaterBillingType { get; set; } = string.Empty;
    public string AllowedTenantType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? BankCode { get; set; }
    public string? BankAccount { get; set; }
    public string? AccountName { get; set; }
    public int RoomCount { get; set; }
    public int AvailableRoomCount { get; set; }
    public string? CertificateStatus { get; set; }   // trạng thái giấy PCCC mới nhất
    public string? CertificateNote { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class HouseDetailDto : HouseDto
{
    public string LandlordName { get; set; } = string.Empty;
    public string? LandlordPhone { get; set; }
    public List<FloorDto> Floors { get; set; } = new();
    public List<ServiceDto> Services { get; set; } = new();
}

public class FloorDto
{
    public int Floor { get; set; }
    public List<RoomListDto> Rooms { get; set; } = new();
}

public class FloorSummaryDto
{
    public int Floor { get; set; }
    public int TotalRooms { get; set; }
    public int Available { get; set; }
    public int Occupied { get; set; }
    public int Maintenance { get; set; }
}

public class CertificateDto
{
    public int Id { get; set; }
    public int BoardingHouseId { get; set; }
    public string HouseName { get; set; } = string.Empty;
    public string LandlordName { get; set; } = string.Empty;
    public string CertificateNo { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ReviewNote { get; set; }
    public DateTime UploadedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
}

public class CreateCertificateDto
{
    [Required(ErrorMessage = "Số giấy chứng nhận là bắt buộc")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Số giấy chứng nhận từ 3 đến 50 ký tự")]
    [RegularExpression(CertificateNoPattern, ErrorMessage = "Số giấy chứng nhận chỉ gồm chữ, số và các ký tự / - . (khoảng trắng), phải có ít nhất một chữ số")]
    public string CertificateNo { get; set; } = string.Empty;
    public const string CertificateNoPattern = @"^(?=.*\d)[0-9A-Za-zÀ-ỹĐđ][0-9A-Za-zÀ-ỹĐđ/.\- ]*$";
    [Required(ErrorMessage = "Vui lòng chọn file giấy chứng nhận")]
    public IFormFile File { get; set; } = null!;
}

public class ReviewCertificateDto : NestU.API.DTOs.Common.DecisionDto { }

public class ServiceDto
{
    public int Id { get; set; }
    public int BoardingHouseId { get; set; }
    [Required(ErrorMessage = "Tên dịch vụ là bắt buộc")] [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    [Range(0, 100_000_000, ErrorMessage = "Giá dịch vụ không hợp lệ")]
    public decimal Price { get; set; }
    /// <summary>PerMonth hoặc PerPerson</summary>
    [Required] [RegularExpression("^(PerMonth|PerPerson)$", ErrorMessage = "Đơn vị tính không hợp lệ")]
    public string Unit { get; set; } = "PerMonth";
    public bool IsActive { get; set; } = true;
}

public class MapMarkerDto
{
    public int HouseId { get; set; }
    public string Name { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public decimal MinPrice { get; set; }
    public int AvailableRooms { get; set; }
}
