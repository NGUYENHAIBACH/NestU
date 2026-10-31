namespace NestU.API.Services.Interfaces;

public interface IFileStorage
{
    /// <summary>Lưu file vào wwwroot/uploads/{folder}, trả về URL tương đối "/uploads/...".</summary>
    Task<string> SaveAsync(IFormFile file, string folder, string[]? allowedExtensions = null, long maxBytes = 5 * 1024 * 1024);
}
