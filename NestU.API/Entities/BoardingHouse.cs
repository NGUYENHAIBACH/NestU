using NestU.API.Entities.Enums;

namespace NestU.API.Entities;

public class BoardingHouse
{
    public int Id { get; set; }
    public string LandlordId { get; set; } = string.Empty;
    public ApplicationUser Landlord { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public int AreaId { get; set; }
    public Area Area { get; set; } = null!;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public int TotalFloors { get; set; } = 1;
    public HouseStatus Status { get; set; } = HouseStatus.Pending;
    public bool IsHidden { get; set; }   // Admin ẩn tin: không hiện công khai, không nhận lịch xem/yêu cầu thuê
    public decimal ElectricityPrice { get; set; }                     // đ/kWh, tối đa 4.000
    public decimal WaterPrice { get; set; }                           // đ/m3 hoặc đ/người
    public WaterBillingType WaterBillingType { get; set; } = WaterBillingType.PerM3;
    public AllowedTenantType AllowedTenantType { get; set; } = AllowedTenantType.All;
    public string? Description { get; set; }
    public string? BankCode { get; set; }
    public string? BankAccount { get; set; }
    public string? AccountName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Room> Rooms { get; set; } = new List<Room>();
    public ICollection<FireSafetyCertificate> Certificates { get; set; } = new List<FireSafetyCertificate>();
    public ICollection<Service> Services { get; set; } = new List<Service>();
}
