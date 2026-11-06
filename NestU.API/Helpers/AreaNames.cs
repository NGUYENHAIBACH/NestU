using System.Text;
using System.Text.RegularExpressions;

namespace NestU.API.Helpers;

/// <summary>
/// Chuẩn hóa tên quận/huyện thành tên khu vực: bỏ tiền tố hành chính, riêng quận đánh số giữ "Quận N".
/// "Quận Bình Thạnh" → "Bình Thạnh", "Thành phố Thủ Đức" → "Thủ Đức", "Quận 12" → "Quận 12".
/// Web App (Form.cshtml của nhà trọ) dùng cùng quy tắc để tự chọn khu vực.
/// </summary>
public static class AreaNames
{
    private static readonly Regex Prefix = new(@"^(quận|huyện|thị xã|thành phố)\s+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex Spaces = new(@"\s+");

    public static string Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
        var name = Spaces.Replace(raw.Normalize(NormalizationForm.FormC).Trim(), " ");
        var rest = Prefix.Replace(name, string.Empty);
        if (rest.Length == 0) return name;
        return rest.All(char.IsDigit) ? "Quận " + rest : rest;
    }

    /// <summary>Khóa so khớp: tên đã chuẩn hóa, không phân biệt hoa thường.</summary>
    public static string Key(string? raw) => Normalize(raw).ToLowerInvariant();
}
