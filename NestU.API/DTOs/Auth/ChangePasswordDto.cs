using System.ComponentModel.DataAnnotations;

namespace NestU.API.DTOs.Auth;

/// <summary>Đổi mật khẩu khi đã đăng nhập: cần mật khẩu hiện tại và mã OTP gửi về email tài khoản.</summary>
public class ChangePasswordDto : IValidatableObject
{
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mã OTP")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "Mã OTP phải gồm đúng 6 chữ số")]
    public string Otp { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới")]
    [MinLength(6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
    public string NewPassword { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.IsNullOrEmpty(NewPassword) && NewPassword == CurrentPassword)
            yield return new ValidationResult("Mật khẩu mới phải khác mật khẩu hiện tại", new[] { nameof(NewPassword) });
    }
}
