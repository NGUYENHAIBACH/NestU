using System.ComponentModel.DataAnnotations;

namespace NestU.WebApp.Models.Houses;

public class LandlordProfileDto
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    [Required(ErrorMessage = "Họ tên là bắt buộc")] [StringLength(100)] [Display(Name = "Họ và tên")] public string FullName { get; set; } = string.Empty;
    [Required] [RegularExpression(@"^\d{12}$", ErrorMessage = "CCCD gồm 12 chữ số")] [Display(Name = "Số CCCD")] public string CitizenId { get; set; } = string.Empty;
    [Required] [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại gồm 10 chữ số")] [Display(Name = "Số điện thoại")] public string Phone { get; set; } = string.Empty;
    public string ApprovalStatus { get; set; } = string.Empty;
    public string? ReviewNote { get; set; }
}

