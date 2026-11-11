using Microsoft.EntityFrameworkCore;
using NestU.API.Data;

namespace NestU.API.Helpers;

/// <summary>
/// Mỗi số điện thoại chỉ gắn với một tài khoản, xét hồ sơ người thuê, hồ sơ chủ trọ và số điện thoại của tài khoản (kể cả Admin).
/// Cùng một tài khoản được giữ số của mình. Số điện thoại người ở cùng (ContractMembers) không xét.
/// </summary>
public static class PhoneNumbers
{
    public static async Task EnsureNotUsedByOtherAsync(NestUDbContext db, string phone, string? userId)
    {
        var p = phone.Trim();
        var used = await db.TenantProfiles.AnyAsync(x => x.Phone == p && x.UserId != userId)
                || await db.LandlordProfiles.AnyAsync(x => x.Phone == p && x.UserId != userId)
                || await db.Users.AnyAsync(u => u.PhoneNumber == p && u.Id != userId);
        if (used) throw ApiException.Conflict("Số điện thoại này đã được dùng cho một tài khoản khác.");
    }
}
