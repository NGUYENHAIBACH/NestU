using NestU.API.Entities.Enums;

namespace NestU.API.Entities;

public class Service
{
    public int Id { get; set; }
    public int BoardingHouseId { get; set; }
    public BoardingHouse BoardingHouse { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public ServiceUnit Unit { get; set; } = ServiceUnit.PerMonth;
    public bool IsActive { get; set; } = true;
}
