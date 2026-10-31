using NestU.API.Entities.Enums;

namespace NestU.API.Entities;

public class FireSafetyCertificate
{
    public int Id { get; set; }
    public int BoardingHouseId { get; set; }
    public BoardingHouse BoardingHouse { get; set; } = null!;
    public string CertificateNo { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public CertificateStatus Status { get; set; } = CertificateStatus.Pending;
    public string? ReviewNote { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
}
