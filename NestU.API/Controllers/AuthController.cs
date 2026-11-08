using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NestU.API.DTOs.Auth;
using NestU.API.Helpers;
using NestU.API.Services.Interfaces;

namespace NestU.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    public AuthController(IAuthService auth) => _auth = auth;

    /// <summary>Đăng ký Tenant (sinh viên / khách thường) hoặc Landlord (chờ Admin duyệt).</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDto>> Register(RegisterDto dto)
    {
        var user = await _auth.RegisterAsync(dto);
        return CreatedAtAction(nameof(Me), null, user);
    }

    /// <summary>Đăng nhập, trả JWT 60 phút kèm role.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(TokenDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TokenDto>> Login(LoginDto dto) => Ok(await _auth.LoginAsync(dto));

    /// <summary>Gửi mã OTP qua email để đặt lại mật khẩu (mã có hiệu lực trong 5 phút).</summary>
    [HttpPost("forgot-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
    {
        await _auth.ForgotPasswordAsync(dto);
        return Ok(new { message = "Mã xác thực OTP đã được gửi đến email của bạn." });
    }

    /// <summary>Xác thực mã OTP và đặt lại mật khẩu mới.</summary>
    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
    {
        await _auth.ResetPasswordAsync(dto);
        return Ok(new { message = "Đặt lại mật khẩu thành công. Vui lòng đăng nhập bằng mật khẩu mới." });
    }

    /// <summary>Thông tin người dùng hiện tại.</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserDto>> Me() => Ok(await _auth.GetMeAsync(User.GetUserId()));
}
