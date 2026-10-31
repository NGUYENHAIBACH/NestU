using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NestU.API.Data;
using NestU.API.Helpers;

namespace NestU.API.Controllers.Landlord;

public class LandlordProfileDto
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    [Required(ErrorMessage = "Họ tên là bắt buộc")] [StringLength(100)] public string FullName { get; set; } = string.Empty;
    [Required] [RegularExpression(@"^\d{12}$", ErrorMessage = "CCCD gồm 12 chữ số")] public string CitizenId { get; set; } = string.Empty;
    [Required] [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại gồm 10 chữ số")] public string Phone { get; set; } = string.Empty;
    public string ApprovalStatus { get; set; } = string.Empty;
    public string? ReviewNote { get; set; }
}

/// <summary>Hồ sơ chủ trọ: không cần policy duyệt, để Landlord chờ duyệt vẫn xem được trạng thái.</summary>
[ApiController]
[Route("api/landlord/profile")]
[Authorize(Roles = Roles.Landlord)]
public class LandlordProfileController : ControllerBase
{
    private readonly NestUDbContext _db;
    public LandlordProfileController(NestUDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<LandlordProfileDto>> Get()
    {
        var uid = User.GetUserId();
        var p = await _db.LandlordProfiles.AsNoTracking().Include(x => x.User).FirstOrDefaultAsync(x => x.UserId == uid)
            ?? throw ApiException.NotFound("Chưa có hồ sơ chủ trọ.");
        return Ok(new LandlordProfileDto
        {
            UserId = uid, Email = p.User.Email ?? "", FullName = p.FullName, CitizenId = p.CitizenId, Phone = p.Phone,
            ApprovalStatus = p.ApprovalStatus.ToString(), ReviewNote = p.ReviewNote
        });
    }

    [HttpPut]
    public async Task<IActionResult> Update(LandlordProfileDto dto)
    {
        var uid = User.GetUserId();
        var p = await _db.LandlordProfiles.Include(x => x.User).FirstOrDefaultAsync(x => x.UserId == uid) ?? throw ApiException.NotFound("Chưa có hồ sơ chủ trọ.");
        p.FullName = dto.FullName.Trim(); p.CitizenId = dto.CitizenId; p.Phone = dto.Phone;
        p.User.FullName = p.FullName; p.User.PhoneNumber = dto.Phone;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
