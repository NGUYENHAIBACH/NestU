using NestU.API.DTOs.Auth;

namespace NestU.API.Services.Interfaces;

public interface IAuthService
{
    Task<UserDto> RegisterAsync(RegisterDto dto);
    Task<TokenDto> LoginAsync(LoginDto dto);
    Task<UserDto> GetMeAsync(string userId);
    Task ForgotPasswordAsync(ForgotPasswordDto dto);
    Task ResetPasswordAsync(ResetPasswordDto dto);
    Task SendChangePasswordOtpAsync(string userId);
    Task ChangePasswordAsync(string userId, ChangePasswordDto dto);
}
