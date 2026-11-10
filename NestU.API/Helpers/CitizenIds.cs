using Microsoft.EntityFrameworkCore;
using NestU.API.Data;

namespace NestU.API.Helpers;

/// <summary>
/// Mỗi số CCCD chỉ gắn với một tài khoản, xét cả hồ sơ người thuê lẫn hồ sơ chủ trọ.
/// Cùng một tài khoản được giữ CCCD của mình (lưu lại hồ sơ, Admin đổi role). CCCD người ở cùng (ContractMembers) không xét.
/// </summary>
public static class CitizenIds
{
    public static async Task EnsureNotUsedByOtherAsync(NestUDbContext db, string citizenId, string? userId)
    {
        var id = citizenId.Trim();
        var used = await db.TenantProfiles.AnyAsync(p => p.CitizenId == id && p.UserId != userId)
                || await db.LandlordProfiles.AnyAsync(p => p.CitizenId == id && p.UserId != userId);
        if (used) throw ApiException.Conflict("Số CCCD này đã được dùng cho một tài khoản khác.");
    }
}
