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
            m.IsStudent, m.StudentCode, m.UniversityId
        });

    public Task<ApiResult<UserDto>> MeAsync() => GetAsync<UserDto>("api/auth/me");
}
