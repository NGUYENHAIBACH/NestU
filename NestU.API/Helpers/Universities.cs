using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using NestU.API.Data;
using NestU.API.Entities;

namespace NestU.API.Helpers;

/// <summary>
/// Sinh viên chọn trường có sẵn (UniversityId) hoặc nhập tay tên trường (UniversityName).
/// Tên nhập tay trùng tên đầy đủ hoặc tên viết tắt của trường có sẵn (không phân biệt hoa thường) thì dùng trường đó,
/// chưa có thì tạo University mới (tọa độ 0, Admin bổ sung sau ở Danh mục) và lưu cùng lần lưu hồ sơ.
/// </summary>
public static class Universities
{
    public const int MaxNameLength = 200;

    public static async Task<University> ResolveAsync(NestUDbContext db, int? universityId, string? universityName)
    {
        if (universityId is not null)
            return await db.Universities.FirstOrDefaultAsync(u => u.Id == universityId)
                ?? throw ApiException.BadRequest("Trường đại học đã chọn không tồn tại.");

        var name = Regex.Replace(universityName ?? string.Empty, @"\s+", " ").Trim();
        if (name.Length == 0) throw ApiException.BadRequest("Sinh viên phải chọn hoặc nhập tên trường.");
        if (name.Length < 3) throw ApiException.BadRequest("Tên trường quá ngắn (tối thiểu 3 ký tự).");
        if (name.Length > MaxNameLength) throw ApiException.BadRequest($"Tên trường tối đa {MaxNameLength} ký tự.");
        if (!name.Any(char.IsLetter)) throw ApiException.BadRequest("Tên trường phải có chữ cái.");

        // SQL Server mặc định so sánh không phân biệt hoa thường
        var existing = await db.Universities.FirstOrDefaultAsync(u => u.Name == name || u.ShortName == name);
        if (existing is not null) return existing;

        var created = new University { Name = name, Latitude = 0, Longitude = 0 };
        db.Universities.Add(created);
        return created;
    }
}
