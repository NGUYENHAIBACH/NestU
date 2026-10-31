namespace NestU.API.Entities;

public class UtilityReading
{
    public int Id { get; set; }
    public int ContractId { get; set; }
    public Contract Contract { get; set; } = null!;
    public int Month { get; set; }
    public int Year { get; set; }
    public int ElectricIndex { get; set; }
    public int WaterIndex { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}
