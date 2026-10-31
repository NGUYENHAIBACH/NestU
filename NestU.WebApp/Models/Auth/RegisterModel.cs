using System.ComponentModel.DataAnnotations;

namespace NestU.WebApp.Models.Auth;

/// <summary>Model cùng tên với RegisterDto bên API, validation riêng (mục 2.8).</summary>
public class RegisterModel : IValidatableObject
{
    [Required(ErrorMessage = "Vui lòng nhập email")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
    [MinLength(6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập lại mật khẩu")]
    [Compare(nameof(Password), ErrorMessage = "Mật khẩu nhập lại không khớp")]
    [DataType(DataType.Password)]
    [Display(Name = "Nhập lại mật khẩu")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập họ tên")]
    [StringLength(100)]
    [Display(Name = "Họ và tên")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
    [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại gồm 10 chữ số, bắt đầu bằng 0")]
    [Display(Name = "Số điện thoại")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số CCCD")]
    [RegularExpression(@"^\d{12}$", ErrorMessage = "CCCD gồm đúng 12 chữ số")]
    [Display(Name = "Số CCCD")]
    public string CitizenId { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Bạn là")]
    public string Role { get; set; } = "Tenant";

    [Display(Name = "Tôi là sinh viên")]
    public bool IsStudent { get; set; }

    [StringLength(20)]
    [Display(Name = "Mã số sinh viên")]
    public string? StudentCode { get; set; }

    [Display(Name = "Trường")]
    public int? UniversityId { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Role == "Tenant" && IsStudent)
        {
            if (string.IsNullOrWhiteSpace(StudentCode))
                yield return new ValidationResult("Sinh viên phải nhập mã số sinh viên", new[] { nameof(StudentCode) });
            if (UniversityId is null)
                yield return new ValidationResult("Sinh viên phải chọn trường", new[] { nameof(UniversityId) });
        }
    }
}
