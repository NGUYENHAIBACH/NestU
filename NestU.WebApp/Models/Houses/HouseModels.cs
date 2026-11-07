using System.ComponentModel.DataAnnotations;
using NestU.WebApp.Models.Rooms;

namespace NestU.WebApp.Models.Houses;

public class CreateHouseDto
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Tên nhà trọ là bắt buộc")] [StringLength(200)] [Display(Name = "Tên nhà trọ")] public string Name { get; set; } = string.Empty;
    [Required(ErrorMessage = "Địa chỉ là bắt buộc")] [StringLength(300)] [Display(Name = "Địa chỉ")] public string Address { get; set; } = string.Empty;
    // Không bắt buộc: để trống thì API suy ra khu vực từ DistrictName (quận/huyện trong địa chỉ)
    [Range(1, int.MaxValue, ErrorMessage = "Khu vực không hợp lệ")] [Display(Name = "Khu vực")] public int? AreaId { get; set; }
    [StringLength(100)] public string? DistrictName { get; set; }
    [Range(-90, 90)] [Display(Name = "Vĩ độ")] public double? Latitude { get; set; }
    [Range(-180, 180)] [Display(Name = "Kinh độ")] public double? Longitude { get; set; }
    [Range(1, 50, ErrorMessage = "Số tầng từ 1 đến 50")] [Display(Name = "Số tầng")] public int TotalFloors { get; set; } = 1;
    // Quy định 2 — kiểm tra ngay ở Web App (mục 2.8)
    [Range(1, 4000, ErrorMessage = "Giá điện tối đa 4.000đ/kWh")] [Display(Name = "Giá điện (đ/kWh, tối đa 4.000)")] public decimal ElectricityPrice { get; set; } = 3500;
    [Range(1, 1_000_000, ErrorMessage = "Giá nước phải lớn hơn 0")] [Display(Name = "Giá nước")] public decimal WaterPrice { get; set; } = 15000;
    [Required] [Display(Name = "Cách tính nước")] public string WaterBillingType { get; set; } = "PerM3";
    [Required] [Display(Name = "Đối tượng thuê")] public string AllowedTenantType { get; set; } = "All";
    [StringLength(2000)] [Display(Name = "Mô tả")] public string? Description { get; set; }
    [StringLength(20)] [Display(Name = "Mã ngân hàng (VCB, TCB...)")] public string? BankCode { get; set; }
    [StringLength(30)] [Display(Name = "Số tài khoản")] public string? BankAccount { get; set; }
    [StringLength(100)] [Display(Name = "Tên chủ tài khoản")] public string? AccountName { get; set; }
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
    public string? CertificateStatus { get; set; }
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
    public int HouseId { get; set; }
    [Required(ErrorMessage = "Số giấy chứng nhận là bắt buộc")] [StringLength(50)] [Display(Name = "Số giấy chứng nhận PCCC")] public string CertificateNo { get; set; } = string.Empty;
    [Required(ErrorMessage = "Vui lòng chọn file")] [Display(Name = "File giấy chứng nhận (ảnh hoặc PDF)")] public IFormFile? File { get; set; }
}

public class ReviewCertificateDto : NestU.WebApp.Models.Common.DecisionDto { }

public class ServiceDto
{
    public int Id { get; set; }
    [Display(Name = "Nhà trọ")] public int BoardingHouseId { get; set; }
    [Required(ErrorMessage = "Tên dịch vụ là bắt buộc")] [StringLength(100)] [Display(Name = "Tên dịch vụ")] public string Name { get; set; } = string.Empty;
    [Range(0, 100_000_000, ErrorMessage = "Giá không hợp lệ")] [Display(Name = "Đơn giá")] public decimal Price { get; set; }
    [Required] [RegularExpression("^(PerMonth|PerPerson)$", ErrorMessage = "Đơn vị tính không hợp lệ")] [Display(Name = "Tính theo")] public string Unit { get; set; } = "PerMonth";
    [Display(Name = "Đang áp dụng")] public bool IsActive { get; set; } = true;
}
