using Microsoft.EntityFrameworkCore;
using NestU.API.Data;

namespace NestU.API.Helpers;

/// <summary>
/// Mỗi mã sinh viên chỉ gắn với một tài khoản người thuê.
/// Định dạng: 2 chữ cái in hoa không dấu + 6 chữ số (VD: SE123456).
/// </summary>
public static class StudentCodes
{
    public const string Pattern = @"^[A-Z]{2}[0-9]{6}$";
    public const string FormatErrorMessage = "Mã sinh viên phải gồm 2 ký tự in hoa không dấu và 6 chữ số (VD: SE123456).";
    public const string DuplicateErrorMessage = "Mã sinh viên này đã được sử dụng cho một tài khoản khác.";

    public static async Task EnsureNotUsedByOtherAsync(NestUDbContext db, string studentCode, string? userId)
    {
        var code = studentCode.Trim();
        var used = await db.TenantProfiles.AnyAsync(p => p.StudentCode == code && p.UserId != userId);
        if (used) throw ApiException.Conflict(DuplicateErrorMessage);
    }
}

