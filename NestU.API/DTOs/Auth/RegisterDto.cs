using System.ComponentModel.DataAnnotations;

namespace NestU.API.DTOs.Auth;

public class RegisterDto : IValidatableObject
{
    [Required(ErrorMessage = "Email là bắt buộc")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
    [MinLength(6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Họ tên là bắt buộc")]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
    [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại phải gồm 10 chữ số, bắt đầu bằng 0")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Số CCCD là bắt buộc")]
    [RegularExpression(@"^\d{12}$", ErrorMessage = "CCCD phải gồm đúng 12 chữ số")]
    public string CitizenId { get; set; } = string.Empty;

    /// <summary>"Tenant" hoặc "Landlord"</summary>
    [Required]
    [RegularExpression("^(Tenant|Landlord)$", ErrorMessage = "Vai trò phải là Tenant hoặc Landlord")]
    public string Role { get; set; } = "Tenant";

    // Chỉ dùng cho Tenant
    public bool IsStudent { get; set; }
    [StringLength(20)]
    public string? StudentCode { get; set; }
    public int? UniversityId { get; set; }
    /// <summary>Tên trường nhập tay khi chưa có trong danh sách; dùng khi UniversityId để trống.</summary>
    [StringLength(200, ErrorMessage = "Tên trường tối đa 200 ký tự")]
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
