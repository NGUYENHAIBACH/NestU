using NestU.WebApp.Models.Auth;
using NestU.WebApp.Models.Common;

namespace NestU.WebApp.ApiClients;

public class AuthApiClient : ApiClientBase
{
    public AuthApiClient(HttpClient http) : base(http) { }

    public Task<ApiResult<TokenDto>> LoginAsync(LoginModel m)
        => PostAsync<TokenDto>("api/auth/login", new { m.Email, m.Password });

    public Task<ApiResult<UserDto>> RegisterAsync(RegisterModel m)
        => PostAsync<UserDto>("api/auth/register", new
        {
            m.Email, m.Password, m.FullName, m.Phone, m.CitizenId, m.Role,
            m.IsStudent, m.StudentCode, m.UniversityId, m.UniversityName
        });

    public Task<ApiResult<UserDto>> MeAsync() => GetAsync<UserDto>("api/auth/me");

    public Task<ApiResult> ForgotPasswordAsync(ForgotPasswordModel m)
        => PostAsync("api/auth/forgot-password", new { m.Email });

    public Task<ApiResult> ResetPasswordAsync(ResetPasswordModel m)
        => PostAsync("api/auth/reset-password", new { m.Email, m.Otp, m.NewPassword });
}
