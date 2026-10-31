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

        var root = Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"), "uploads", folder);
        Directory.CreateDirectory(root);
        var name = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}{ext}";
        await using var fs = File.Create(Path.Combine(root, name));
        await file.CopyToAsync(fs);
        return $"/uploads/{folder}/{name}";
    }
}
