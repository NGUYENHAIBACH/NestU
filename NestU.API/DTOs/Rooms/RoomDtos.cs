using System.ComponentModel.DataAnnotations;
using NestU.API.DTOs.Assets;

namespace NestU.API.DTOs.Rooms;

public class CreateRoomDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn nhà trọ")]
    public int BoardingHouseId { get; set; }

    [Required(ErrorMessage = "Số phòng là bắt buộc")] [StringLength(10)]
    public string RoomNumber { get; set; } = string.Empty;

    [Range(1, 50, ErrorMessage = "Tầng không hợp lệ")]
    public int Floor { get; set; } = 1;

    [Range(1, 500, ErrorMessage = "Diện tích phải lớn hơn 0")]
    public decimal Area { get; set; }

    [Range(1, 1_000_000_000, ErrorMessage = "Giá phòng phải lớn hơn 0")]
    public decimal Price { get; set; }

    [Range(0, 1_000_000_000, ErrorMessage = "Tiền cọc không hợp lệ")]
    public decimal? DepositAmount { get; set; }   // null → bằng 1 tháng tiền phòng

    [Range(1, 10, ErrorMessage = "Sức chứa từ 1 đến 10 người")]
    public int Capacity { get; set; } = 1;

    [StringLength(2000)] public string? Description { get; set; }
    public List<int> AmenityIds { get; set; } = new();
    public List<string> ImageUrls { get; set; } = new();
    public List<AssetTemplateDto> Assets { get; set; } = new();
}

public class UpdateRoomDto
{
    [Required(ErrorMessage = "Số phòng là bắt buộc")] [StringLength(10)]
    public string RoomNumber { get; set; } = string.Empty;
    [Range(1, 50, ErrorMessage = "Tầng không hợp lệ")] public int Floor { get; set; } = 1;
    [Range(1, 500, ErrorMessage = "Diện tích phải lớn hơn 0")] public decimal Area { get; set; }
    [Range(1, 1_000_000_000, ErrorMessage = "Giá phòng phải lớn hơn 0")] public decimal Price { get; set; }
    [Range(0, 1_000_000_000)] public decimal DepositAmount { get; set; }
    [Range(1, 10, ErrorMessage = "Sức chứa từ 1 đến 10 người")] public int Capacity { get; set; } = 1;
    [StringLength(2000)] public string? Description { get; set; }
    public List<int> AmenityIds { get; set; } = new();
    public List<string> ImageUrls { get; set; } = new();
}

public class BulkCreateRoomsDto
{
    [Range(1, 50, ErrorMessage = "Tầng không hợp lệ")] public int Floor { get; set; }
    [Range(1, 99, ErrorMessage = "Số bắt đầu từ 1 đến 99")] public int FromNumber { get; set; } = 1;
    [Range(1, 99, ErrorMessage = "Số kết thúc từ 1 đến 99")] public int ToNumber { get; set; } = 1;
    [Range(1, 500, ErrorMessage = "Diện tích phải lớn hơn 0")] public decimal Area { get; set; }
    [Range(1, 1_000_000_000, ErrorMessage = "Giá phòng phải lớn hơn 0")] public decimal Price { get; set; }
    [Range(0, 1_000_000_000)] public decimal? DepositAmount { get; set; }
    [Range(1, 10, ErrorMessage = "Sức chứa từ 1 đến 10 người")] public int Capacity { get; set; } = 1;
    [StringLength(2000)] public string? Description { get; set; }
    public List<int> AmenityIds { get; set; } = new();
    public List<AssetTemplateDto> Assets { get; set; } = new();
}

public class SetRoomStatusDto
{
    /// <summary>Available hoặc Maintenance</summary>
    [Required] [RegularExpression("^(Available|Maintenance)$", ErrorMessage = "Chỉ được đặt Available hoặc Maintenance")]
    public string Status { get; set; } = "Available";
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
    public List<Houses.ServiceDto> Services { get; set; } = new();
    public double? AverageRating { get; set; }
    public int ReviewCount { get; set; }
}
