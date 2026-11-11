using System.ComponentModel.DataAnnotations;

namespace NestU.WebApp.Models.Auth;

/// <summary>Đổi mật khẩu trong trang hồ sơ: mật khẩu hiện tại + mã OTP gửi về email.</summary>
public class ChangePasswordModel : IValidatableObject
{
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu hiện tại")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mã OTP")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "Mã OTP phải gồm đúng 6 chữ số")]
    [Display(Name = "Mã xác thực (OTP)")]
    public string Otp { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới")]
    [MinLength(6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu mới")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập lại mật khẩu mới")]
    [Compare(nameof(NewPassword), ErrorMessage = "Mật khẩu nhập lại không khớp")]
    [DataType(DataType.Password)]
    [Display(Name = "Nhập lại mật khẩu mới")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.IsNullOrEmpty(NewPassword) && NewPassword == CurrentPassword)
            yield return new ValidationResult("Mật khẩu mới phải khác mật khẩu hiện tại", new[] { nameof(NewPassword) });
    }
}

/// <summary>Bí danh tương ứng với ChangePasswordDto bên API theo Quy tắc 2 CLAUDE.md.</summary>
public class ChangePasswordDto : ChangePasswordModel
{
}
