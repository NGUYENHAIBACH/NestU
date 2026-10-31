namespace NestU.API.DTOs.Auth;

public class TokenDto
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    /// <summary>Chỉ có với Landlord: Pending / Approved / Rejected</summary>
    public string? LandlordApprovalStatus { get; set; }
}
