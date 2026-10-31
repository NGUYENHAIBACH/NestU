using Microsoft.AspNetCore.Identity;

namespace NestU.API.Entities;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public bool IsLocked { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public TenantProfile? TenantProfile { get; set; }
    public LandlordProfile? LandlordProfile { get; set; }
}
