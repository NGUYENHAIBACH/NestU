using NestU.API.DTOs.Auth;

namespace NestU.API.Services.Interfaces;

public interface IAuthService
{
    Task<UserDto> RegisterAsync(RegisterDto dto);
    Task<TokenDto> LoginAsync(LoginDto dto);
    Task<UserDto> GetMeAsync(string userId);
}
