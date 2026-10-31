namespace NestU.API.Entities;

public class TenantProfile
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    public string FullName { get; set; } = string.Empty;
    public string CitizenId { get; set; } = string.Empty;   // CCCD 12 số
    public string Phone { get; set; } = string.Empty;        // 10 số
    public bool IsStudent { get; set; }
    public string? StudentCode { get; set; }
    public int? UniversityId { get; set; }
    public University? University { get; set; }
}
