using System.ComponentModel.DataAnnotations;

namespace NestU.API.DTOs.Auth;

public class ResetPasswordDto
{
    [Required(ErrorMessage = "Vui lòng nhập email")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mã OTP")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "Mã OTP phải gồm đúng 6 chữ số")]
    public string Otp { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới")]
    [MinLength(6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
    public string NewPassword { get; set; } = string.Empty;
}

