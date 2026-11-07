using System.ComponentModel.DataAnnotations;

namespace NestU.WebApp.Models.Auth;

public class ForgotPasswordModel
{
    [Required(ErrorMessage = "Vui lòng nhập email")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [Display(Name = "Email đã đăng ký")]
    public string Email { get; set; } = string.Empty;
}

/// <summary>Bí danh tương ứng với ForgotPasswordDto bên API theo Quy tắc 2 CLAUDE.md.</summary>
public class ForgotPasswordDto : ForgotPasswordModel
{
}

