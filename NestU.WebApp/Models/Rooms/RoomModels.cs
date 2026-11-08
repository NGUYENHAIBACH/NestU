using System.ComponentModel.DataAnnotations;
using NestU.WebApp.Models.Assets;
using NestU.WebApp.Models.Houses;

namespace NestU.WebApp.Models.Rooms;

public class RoomListDto
{
    public int Id { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public int Floor { get; set; }
    public decimal Area { get; set; }
    public decimal Price { get; set; }
    public decimal DepositAmount { get; set; }
    public int Capacity { get; set; }
    public string Status { get; set; } = string.Empty;
    public string AvailabilityLabel { get; set; } = string.Empty;
    public int ViewCount { get; set; }
    public string? ThumbnailUrl { get; set; }
    public int HouseId { get; set; }
    public string HouseName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public int AreaId { get; set; }
    public string AreaName { get; set; } = string.Empty;
    public decimal ElectricityPrice { get; set; }
    public bool StudentOnly { get; set; }
    public bool FireSafetyApproved { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class RoomDetailDto : RoomListDto
{
    public string? Description { get; set; }
    public decimal WaterPrice { get; set; }
    public string WaterBillingType { get; set; } = string.Empty;
    public int TotalFloors { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string LandlordName { get; set; } = string.Empty;
    public List<string> ImageUrls { get; set; } = new();
    public List<string> Amenities { get; set; } = new();
    public List<int> AmenityIds { get; set; } = new();
    public List<RoomAssetDto> Assets { get; set; } = new();
    public List<ServiceDto> Services { get; set; } = new();
    public double? AverageRating { get; set; }
    public int ReviewCount { get; set; }
}

/// <summary>Bộ lọc trang Tìm phòng → dịch sang OData $filter.</summary>
public class RoomSearchModel
{
    [Display(Name = "Khu vực")] public int? AreaId { get; set; }
    [Display(Name = "Giá từ")] public decimal? MinPrice { get; set; }
    [Display(Name = "Giá đến")] public decimal? MaxPrice { get; set; }
    [Display(Name = "Diện tích tối thiểu")] public decimal? MinArea { get; set; }
    [Display(Name = "Tầng")] public int? Floor { get; set; }
    [Display(Name = "Chỉ phòng còn trống")] public bool OnlyAvailable { get; set; } = true;
    [Display(Name = "Sắp xếp")] public string Sort { get; set; } = "newest";   // newest | price_asc | price_desc | views
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
    public string? Keyword { get; set; }
}

public class CreateRoomDto
{
    public int BoardingHouseId { get; set; }
    [Required(ErrorMessage = "Số phòng là bắt buộc")] [StringLength(10)] [Display(Name = "Số phòng")] public string RoomNumber { get; set; } = string.Empty;
    [Range(1, 50, ErrorMessage = "Tầng không hợp lệ")] [Display(Name = "Tầng")] public int Floor { get; set; } = 1;
    [Range(1, 500, ErrorMessage = "Diện tích phải lớn hơn 0")] [Display(Name = "Diện tích (m²)")] public decimal Area { get; set; }
    [Range(1, 1_000_000_000, ErrorMessage = "Giá phòng phải lớn hơn 0")] [Display(Name = "Giá phòng (đ/tháng)")] public decimal Price { get; set; }
    [Range(0, 1_000_000_000)] [Display(Name = "Tiền cọc (để trống = 1 tháng)")] public decimal? DepositAmount { get; set; }
    [Range(1, 10, ErrorMessage = "Sức chứa từ 1 đến 10 người")] [Display(Name = "Sức chứa (người)")] public int Capacity { get; set; } = 2;
    [StringLength(2000)] [Display(Name = "Mô tả")] public string? Description { get; set; }
    [Display(Name = "Tiện ích")] public List<int> AmenityIds { get; set; } = new();
    [Display(Name = "Link ảnh (mỗi dòng một link)")] public string? ImageUrlsText { get; set; }
    public List<string> ImageUrls => (ImageUrlsText ?? "").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    public List<AssetTemplateDto> Assets { get; set; } = new();
}

public class UpdateRoomDto
{
    public int Id { get; set; }
    public int BoardingHouseId { get; set; }
    public int TotalFloors { get; set; }
    [Required(ErrorMessage = "Số phòng là bắt buộc")] [StringLength(10)] [Display(Name = "Số phòng")] public string RoomNumber { get; set; } = string.Empty;
    [Range(1, 50, ErrorMessage = "Tầng không hợp lệ")] [Display(Name = "Tầng")] public int Floor { get; set; } = 1;
    [Range(1, 500, ErrorMessage = "Diện tích phải lớn hơn 0")] [Display(Name = "Diện tích (m²)")] public decimal Area { get; set; }
    [Range(1, 1_000_000_000, ErrorMessage = "Giá phòng phải lớn hơn 0")] [Display(Name = "Giá phòng (đ/tháng)")] public decimal Price { get; set; }
    [Range(0, 1_000_000_000)] [Display(Name = "Tiền cọc")] public decimal DepositAmount { get; set; }
    [Range(1, 10, ErrorMessage = "Sức chứa từ 1 đến 10 người")] [Display(Name = "Sức chứa (người)")] public int Capacity { get; set; } = 2;
    [StringLength(2000)] [Display(Name = "Mô tả")] public string? Description { get; set; }
    [Display(Name = "Tiện ích")] public List<int> AmenityIds { get; set; } = new();
    [Display(Name = "Link ảnh (mỗi dòng một link)")] public string? ImageUrlsText { get; set; }
    public List<string> ImageUrls => (ImageUrlsText ?? "").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
}

public class BulkCreateRoomsDto
{
    public int HouseId { get; set; }
    public int TotalFloors { get; set; }
    [Range(1, 50, ErrorMessage = "Tầng không hợp lệ")] [Display(Name = "Tầng")] public int Floor { get; set; } = 1;
    [Range(1, 99)] [Display(Name = "Từ số")] public int FromNumber { get; set; } = 1;
    [Range(1, 99)] [Display(Name = "Đến số")] public int ToNumber { get; set; } = 4;
    [Range(1, 500, ErrorMessage = "Diện tích phải lớn hơn 0")] [Display(Name = "Diện tích (m²)")] public decimal Area { get; set; } = 20;
    [Range(1, 1_000_000_000, ErrorMessage = "Giá phòng phải lớn hơn 0")] [Display(Name = "Giá phòng (đ/tháng)")] public decimal Price { get; set; }
    [Range(0, 1_000_000_000)] [Display(Name = "Tiền cọc (để trống = 1 tháng)")] public decimal? DepositAmount { get; set; }
    [Range(1, 10)] [Display(Name = "Sức chứa (người)")] public int Capacity { get; set; } = 2;
    [StringLength(2000)] [Display(Name = "Mô tả chung")] public string? Description { get; set; }
    [Display(Name = "Tiện ích")] public List<int> AmenityIds { get; set; } = new();
    public List<AssetTemplateDto> Assets { get; set; } = new();
    public IEnumerable<string> PreviewNumbers => FromNumber <= ToNumber ? Enumerable.Range(FromNumber, ToNumber - FromNumber + 1).Select(n => (Floor * 100 + n).ToString()) : Enumerable.Empty<string>();
}

public class SetRoomStatusDto
{
    public int Id { get; set; }
    [Required]
    [RegularExpression("^(Available|Maintenance)$", ErrorMessage = "Chỉ được đặt Available hoặc Maintenance")]
    public string Status { get; set; } = "Available";
}
