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

    [StringLength(200, MinimumLength = 3, ErrorMessage = "Tên trường từ 3 đến 200 ký tự")]
    [Display(Name = "Trường")]
    public string? UniversityName { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Role == "Tenant" && IsStudent)
        {
            if (string.IsNullOrWhiteSpace(StudentCode))
                yield return new ValidationResult("Sinh viên phải nhập mã số sinh viên", new[] { nameof(StudentCode) });
            else if (!System.Text.RegularExpressions.Regex.IsMatch(StudentCode.Trim(), @"^[A-Z]{2}[0-9]{6}$"))
                yield return new ValidationResult("Mã sinh viên phải gồm 2 ký tự in hoa không dấu và 6 chữ số (VD: SE123456)", new[] { nameof(StudentCode) });

            if (UniversityId is null && string.IsNullOrWhiteSpace(UniversityName))
                yield return new ValidationResult("Sinh viên phải chọn hoặc nhập tên trường", new[] { nameof(UniversityName) });
        }
    }
}

/// <summary>Bí danh tương ứng với RegisterDto bên API theo Quy tắc 2 CLAUDE.md.</summary>
public class RegisterDto : RegisterModel
{
}
