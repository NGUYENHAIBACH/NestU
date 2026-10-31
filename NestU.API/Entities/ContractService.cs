namespace NestU.API.Entities;

public class ContractService
{
    public int ContractId { get; set; }
    public Contract Contract { get; set; } = null!;
    public int ServiceId { get; set; }
    public Service Service { get; set; } = null!;
}
