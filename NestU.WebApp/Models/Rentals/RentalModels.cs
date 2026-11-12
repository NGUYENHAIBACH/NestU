using System.ComponentModel.DataAnnotations;
using NestU.WebApp.Models.Houses;

namespace NestU.WebApp.Models.Rentals;

public class TenantProfileDto : IValidatableObject
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    [Required(ErrorMessage = "Họ tên là bắt buộc")] [StringLength(100)] [Display(Name = "Họ và tên")] public string FullName { get; set; } = string.Empty;
    [Required] [RegularExpression(@"^\d{12}$", ErrorMessage = "CCCD gồm 12 chữ số")] [Display(Name = "Số CCCD")] public string CitizenId { get; set; } = string.Empty;
    [Required] [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại gồm 10 chữ số")] [Display(Name = "Số điện thoại")] public string Phone { get; set; } = string.Empty;
    [Display(Name = "Tôi là sinh viên")] public bool IsStudent { get; set; }
    [StringLength(20)] [Display(Name = "Mã số sinh viên")] public string? StudentCode { get; set; }
    [Display(Name = "Trường")] public int? UniversityId { get; set; }
    [StringLength(200, MinimumLength = 3, ErrorMessage = "Tên trường từ 3 đến 200 ký tự")] [Display(Name = "Trường")] public string? UniversityName { get; set; }

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
    public int RoomId { get; set; }
    public string? RoomLabel { get; set; }
    [Required(ErrorMessage = "Vui lòng chọn thời gian")] [Display(Name = "Thời gian xem phòng")] public DateTime ScheduledAt { get; set; } = DateTime.Now.AddDays(1).Date.AddHours(9);
    [StringLength(500)] [Display(Name = "Ghi chú")] public string? Note { get; set; }

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
    public int RoomId { get; set; }
    public string? RoomLabel { get; set; }
    public int Capacity { get; set; }
    public decimal Price { get; set; }
    public decimal DepositAmount { get; set; }
    [Range(1, 10, ErrorMessage = "Số người từ 1 đến 10")] [Display(Name = "Số người ở")] public int NumberOfOccupants { get; set; } = 1;
    [Required(ErrorMessage = "Vui lòng chọn ngày dọn vào")] [DataType(DataType.Date)] [Display(Name = "Ngày dọn vào")] public DateTime MoveInDate { get; set; } = DateTime.Today.AddDays(3);
    [Range(1, 36, ErrorMessage = "Thời hạn từ 1 đến 36 tháng")] [Display(Name = "Thời hạn thuê (tháng)")] public int DurationMonths { get; set; } = 6;
    [StringLength(500)] [Display(Name = "Ghi chú cho chủ trọ")] public string? Note { get; set; }

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
    [Required(ErrorMessage = "Họ tên là bắt buộc")] [StringLength(100)] [Display(Name = "Họ tên")] public string FullName { get; set; } = string.Empty;
    [Required] [RegularExpression(@"^\d{12}$", ErrorMessage = "CCCD gồm 12 chữ số")] [Display(Name = "CCCD")] public string CitizenId { get; set; } = string.Empty;
    [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại gồm 10 chữ số")] [Display(Name = "Điện thoại")] public string? Phone { get; set; }
}

public class ApproveRentalDto
{
    public int RequestId { get; set; }
    public RentalRequestDto? Request { get; set; }
    [DataType(DataType.Date)] [Display(Name = "Ngày bắt đầu")] public DateTime? StartDate { get; set; }
    [Range(1, 36)] [Display(Name = "Thời hạn (tháng)")] public int? DurationMonths { get; set; }
    [Range(0, 1_000_000_000)] [Display(Name = "Tiền cọc đã nhận")] public decimal? DepositAmount { get; set; }
    [Display(Name = "Đã nhận tiền cọc")] public bool DepositPaid { get; set; } = true;
    [Range(0, 1_000_000, ErrorMessage = "Chỉ số điện không hợp lệ")] [Display(Name = "Chỉ số điện nhận phòng")] public int InitialElectricIndex { get; set; } = 0;
    [Range(0, 1_000_000, ErrorMessage = "Chỉ số nước không hợp lệ")] [Display(Name = "Chỉ số nước nhận phòng")] public int InitialWaterIndex { get; set; } = 0;
    [Display(Name = "Dịch vụ đăng ký")] public List<int> ServiceIds { get; set; } = new();
    public List<ContractMemberDto> Members { get; set; } = new();
    public List<ServiceDto> AvailableServices { get; set; } = new();
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
    public string DisplayStatus { get; set; } = string.Empty;
    public DateTime? TerminatedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<ContractMemberDto> Members { get; set; } = new();
    public List<ServiceDto> Services { get; set; } = new();
    public bool CheckInConfirmed { get; set; }
    public bool HasCheckout { get; set; }
    public DepositSettlementDto? DepositSettlement { get; set; }
    public int UnpaidInvoices { get; set; }
}

public class ExtendContractDto
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Vui lòng chọn ngày kết thúc mới")] [DataType(DataType.Date)] [Display(Name = "Ngày kết thúc mới")] public DateTime NewEndDate { get; set; }
}

public class CreateContractChangeDto
{
    public int ContractId { get; set; }
    public string Type { get; set; } = "Renew";  // Renew | Terminate
    [DataType(DataType.Date)] [Display(Name = "Ngày kết thúc mới")] public DateTime? NewEndDate { get; set; }
    [StringLength(500)] [Display(Name = "Lý do")] public string? Reason { get; set; }
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
    public int ContractId { get; set; }
    [StringLength(500)] [Display(Name = "Ghi chú")] public string? Note { get; set; }
    [Display(Name = "Trừ hóa đơn chưa thanh toán vào cọc")] public bool DeductUnpaidInvoices { get; set; } = true;
}
