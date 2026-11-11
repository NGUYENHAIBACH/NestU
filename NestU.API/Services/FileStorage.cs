using NestU.API.Helpers;
using NestU.API.Services.Interfaces;

namespace NestU.API.Services;

public class FileStorage : IFileStorage
{
    private readonly IWebHostEnvironment _env;
    public FileStorage(IWebHostEnvironment env) => _env = env;

    public async Task<string> SaveAsync(IFormFile file, string folder, string[]? allowedExtensions = null, long maxBytes = 5 * 1024 * 1024)
    {
        if (file.Length == 0) throw ApiException.BadRequest("File rỗng.");
        if (file.Length > maxBytes) throw ApiException.BadRequest($"File vượt quá {maxBytes / 1024 / 1024} MB.");
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        allowedExtensions ??= new[] { ".jpg", ".jpeg", ".png", ".webp", ".pdf" };
        if (!allowedExtensions.Contains(ext)) throw ApiException.BadRequest("Định dạng file không được hỗ trợ.");
        if (!await HasValidSignatureAsync(file, ext))
            throw ApiException.BadRequest("Nội dung file không khớp với định dạng (file hỏng hoặc bị đổi đuôi).");

        var safeFolder = Path.GetFileName(folder);
        var root = Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"), "uploads", safeFolder);
        Directory.CreateDirectory(root);
        var name = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}{ext}";
        await using var fs = File.Create(Path.Combine(root, name));
        await file.CopyToAsync(fs);
        return $"/uploads/{safeFolder}/{name}";
    }

    /// <summary>Đọc vài byte đầu để chắc file đúng là ảnh/PDF như phần mở rộng, chặn file khác bị đổi đuôi.</summary>
    private static async Task<bool> HasValidSignatureAsync(IFormFile file, string ext)
    {
        var head = new byte[12];
        await using (var s = file.OpenReadStream())
        {
            var read = 0;
            while (read < head.Length)
            {
                var n = await s.ReadAsync(head.AsMemory(read));
                if (n == 0) break;
                read += n;
            }
            if (read < 4) return false;
        }
        return ext switch
        {
            ".jpg" or ".jpeg" => head[0] == 0xFF && head[1] == 0xD8 && head[2] == 0xFF,
            ".png" => head[0] == 0x89 && head[1] == 0x50 && head[2] == 0x4E && head[3] == 0x47,
            ".webp" => head[0] == 'R' && head[1] == 'I' && head[2] == 'F' && head[3] == 'F' && head[8] == 'W' && head[9] == 'E' && head[10] == 'B' && head[11] == 'P',
            ".pdf" => head[0] == '%' && head[1] == 'P' && head[2] == 'D' && head[3] == 'F',
            _ => true
        };
    }
}
