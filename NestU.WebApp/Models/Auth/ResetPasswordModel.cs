using System.ComponentModel.DataAnnotations;

namespace NestU.WebApp.Models.Auth;

public class ResetPasswordModel
{
    [Required(ErrorMessage = "Vui lòng nhập email")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mã OTP")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "Mã OTP phải gồm đúng 6 chữ số")]
    [Display(Name = "Mã xác thực (OTP)")]
    public string Otp { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới")]
    [MinLength(6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu mới")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng xác nhận lại mật khẩu mới")]
    [Compare(nameof(NewPassword), ErrorMessage = "Mật khẩu nhập lại không khớp")]
    [DataType(DataType.Password)]
    [Display(Name = "Nhập lại mật khẩu mới")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

/// <summary>Bí danh tương ứng với ResetPasswordDto bên API theo Quy tắc 2 CLAUDE.md.</summary>
public class ResetPasswordDto : ResetPasswordModel
{
}

