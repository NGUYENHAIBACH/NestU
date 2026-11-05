using System.ComponentModel.DataAnnotations;

namespace NestU.WebApp.Models.Auth;

public class LoginModel
{
    [Required(ErrorMessage = "Vui lòng nhập email")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

/// <summary>Bí danh tương ứng với LoginDto bên API theo Quy tắc 2 CLAUDE.md.</summary>
public class LoginDto : LoginModel
{
}
