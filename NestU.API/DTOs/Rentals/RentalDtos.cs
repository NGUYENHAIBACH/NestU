using System.ComponentModel.DataAnnotations;
using NestU.API.DTOs.Assets;

namespace NestU.API.DTOs.Rentals;

public class TenantProfileDto : IValidatableObject
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    [Required(ErrorMessage = "Họ tên là bắt buộc")] [StringLength(100)] public string FullName { get; set; } = string.Empty;
    [Required] [RegularExpression(@"^\d{12}$", ErrorMessage = "CCCD gồm 12 chữ số")] public string CitizenId { get; set; } = string.Empty;
    [Required] [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại gồm 10 chữ số")] public string Phone { get; set; } = string.Empty;
    public bool IsStudent { get; set; }
    [StringLength(20)] public string? StudentCode { get; set; }
    public int? UniversityId { get; set; }
    /// <summary>Tên trường; nhập tay khi chưa có trong danh sách (dùng khi UniversityId để trống).</summary>
    [StringLength(200, ErrorMessage = "Tên trường tối đa 200 ký tự")] public string? UniversityName { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (IsStudent)
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

public class CreateAppointmentDto : IValidatableObject
{
    [Range(1, int.MaxValue)] public int RoomId { get; set; }
    [Required(ErrorMessage = "Vui lòng chọn thời gian")] public DateTime ScheduledAt { get; set; }
    [StringLength(500)] public string? Note { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ScheduledAt < DateTime.Now.AddHours(1))
            yield return new ValidationResult("Thời gian xem phải sau thời điểm hiện tại ít nhất 1 giờ.", new[] { nameof(ScheduledAt) });
    }
}

public class AppointmentDto
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public string HouseName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
    public string? TenantPhone { get; set; }
    public DateTime ScheduledAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Note { get; set; }
    public string? RejectReason { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateRentalRequestDto : IValidatableObject
{
    [Range(1, int.MaxValue)] public int RoomId { get; set; }
    [Range(1, 10, ErrorMessage = "Số người từ 1 đến 10")] public int NumberOfOccupants { get; set; } = 1;
    [Required(ErrorMessage = "Vui lòng chọn ngày dọn vào")] public DateTime MoveInDate { get; set; }
    [Range(1, 36, ErrorMessage = "Thời hạn từ 1 đến 36 tháng")] public int DurationMonths { get; set; } = 6;
    [StringLength(500)] public string? Note { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MoveInDate.Date < DateTime.Today)
            yield return new ValidationResult("Ngày dọn vào không được ở quá khứ.", new[] { nameof(MoveInDate) });
    }
}

public class RentalRequestDto
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public string HouseName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal DepositAmount { get; set; }
    public int Capacity { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
    public string? TenantPhone { get; set; }
    public bool TenantIsStudent { get; set; }
    public int NumberOfOccupants { get; set; }
    public DateTime MoveInDate { get; set; }
    public int DurationMonths { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Note { get; set; }
    public string? RejectReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? ContractId { get; set; }
}

public class ContractMemberDto
{
    [Required(ErrorMessage = "Họ tên là bắt buộc")] [StringLength(100)] public string FullName { get; set; } = string.Empty;
    [Required] [RegularExpression(@"^\d{12}$", ErrorMessage = "CCCD gồm 12 chữ số")] public string CitizenId { get; set; } = string.Empty;
    [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại gồm 10 chữ số")] public string? Phone { get; set; }
}

public class ApproveRentalDto
{
    /// <summary>Ngày bắt đầu hợp đồng; null → lấy MoveInDate của yêu cầu.</summary>
    public DateTime? StartDate { get; set; }
    /// <summary>Số tháng; null → DurationMonths của yêu cầu.</summary>
    [Range(1, 36)] public int? DurationMonths { get; set; }
    /// <summary>Tiền cọc đã nhận; null → Room.DepositAmount.</summary>
    [Range(0, 1_000_000_000)] public decimal? DepositAmount { get; set; }
    public bool DepositPaid { get; set; } = true;
    [Range(0, 1_000_000, ErrorMessage = "Chỉ số điện ban đầu không hợp lệ")] public int InitialElectricIndex { get; set; } = 0;
    [Range(0, 1_000_000, ErrorMessage = "Chỉ số nước ban đầu không hợp lệ")] public int InitialWaterIndex { get; set; } = 0;
    public List<int> ServiceIds { get; set; } = new();
    public List<ContractMemberDto> Members { get; set; } = new();
}

public class ContractDto
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public int Floor { get; set; }
    public int HouseId { get; set; }
    public string HouseName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
    public string? TenantPhone { get; set; }
    public string LandlordName { get; set; } = string.Empty;
    public string? LandlordPhone { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal MonthlyRent { get; set; }
    public decimal DepositAmount { get; set; }
    public DateTime? DepositPaidAt { get; set; }
    public int InitialElectricIndex { get; set; }
    public int InitialWaterIndex { get; set; }
    public string Status { get; set; } = string.Empty;
    /// <summary>Active quá EndDate → "Expired" (tính khi đọc, không lưu).</summary>
    public string DisplayStatus { get; set; } = string.Empty;
    public DateTime? TerminatedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<ContractMemberDto> Members { get; set; } = new();
    public List<Houses.ServiceDto> Services { get; set; } = new();
    public bool CheckInConfirmed { get; set; }
    public bool HasCheckout { get; set; }
    public DepositSettlementDto? DepositSettlement { get; set; }
    public int UnpaidInvoices { get; set; }
}

public class ExtendContractDto
{
    [Required(ErrorMessage = "Vui lòng chọn ngày kết thúc mới")] public DateTime NewEndDate { get; set; }
}

public class CreateContractChangeDto
{
    public DateTime? NewEndDate { get; set; }   // Renew
    [StringLength(500)] public string? Reason { get; set; }
}

public class ContractChangeRequestDto
{
    public int Id { get; set; }
    public int ContractId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public string HouseName { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTime? NewEndDate { get; set; }
    public string? Reason { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? DecisionNote { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DecidedAt { get; set; }
}

public class DepositSettlementDto
{
    public int ContractId { get; set; }
    public decimal DepositAmount { get; set; }
    public decimal DamageDeduction { get; set; }
    public decimal UnpaidDeduction { get; set; }
    public decimal RefundAmount { get; set; }
    public string? Note { get; set; }
    public DateTime SettledAt { get; set; }
}

public class CreateDepositSettlementDto
{
    [StringLength(500)] public string? Note { get; set; }
    /// <summary>Có trừ hóa đơn chưa thanh toán vào cọc không (mặc định có).</summary>
    public bool DeductUnpaidInvoices { get; set; } = true;
}
