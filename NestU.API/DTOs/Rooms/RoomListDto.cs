namespace NestU.API.DTOs.Rooms;

/// <summary>DTO phẳng cho OData /odata/Rooms và các danh sách phòng công khai.</summary>
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
    public string AvailabilityLabel { get; set; } = string.Empty;   // "Còn phòng" / "Hết phòng" / "Đang sửa"
    public int ViewCount { get; set; }
    public string? ThumbnailUrl { get; set; }
    public int HouseId { get; set; }
    public string HouseName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public int AreaId { get; set; }
    public string AreaName { get; set; } = string.Empty;
    public decimal ElectricityPrice { get; set; }
    public bool StudentOnly { get; set; }
    public bool FireSafetyApproved { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}
