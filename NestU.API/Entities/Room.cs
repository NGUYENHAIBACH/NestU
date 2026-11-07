using NestU.API.Entities.Enums;

namespace NestU.API.Entities;

public class Room
{
    public int Id { get; set; }
    public int BoardingHouseId { get; set; }
    public BoardingHouse BoardingHouse { get; set; } = null!;
    public string RoomNumber { get; set; } = string.Empty;
    public int Floor { get; set; } = 1;
    public decimal Area { get; set; }            // m2
    public decimal Price { get; set; }           // đ/tháng
    public decimal DepositAmount { get; set; }   // mặc định = 1 tháng tiền phòng
    public int Capacity { get; set; } = 1;
    public RoomStatus Status { get; set; } = RoomStatus.Available;
    public string? Address { get; set; }
    public string? Description { get; set; }
    public int ViewCount { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<RoomImage> Images { get; set; } = new List<RoomImage>();
    public ICollection<RoomAmenity> RoomAmenities { get; set; } = new List<RoomAmenity>();
    public ICollection<RoomAsset> Assets { get; set; } = new List<RoomAsset>();
}
