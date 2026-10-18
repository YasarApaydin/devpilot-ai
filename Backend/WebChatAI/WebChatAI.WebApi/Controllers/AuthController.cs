using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WebChatAI.Application.Common;
using WebChatAI.Application.DTOs;
using WebChatAI.Application.Features.Auth;
using WebChatAI.WebApi.Abstractions;

namespace WebChatAI.WebApi.Controllers;

public class AuthController : ApiController
{
    private readonly IHostEnvironment _env;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IMediator mediator, IHostEnvironment env, ILogger<AuthController> logger) : base(mediator)
    {
        _env = env;
        _logger = logger;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("AuthPolicy")]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(command, cancellationToken);
        if (!result.Success && (result.Message?.Contains("zaten") == true || result.Message?.Contains("already exists") == true))
        {
            return Conflict(result);
        }
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("AuthPolicy")]
    public async Task<IActionResult> Login([FromBody] LoginCommand command, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(command, cancellationToken);
        if (!result.Success || result.Data == null)
        {
            if (result.Message?.Contains("kilitlenmiştir") == true)
            {
                return StatusCode(StatusCodes.Status429TooManyRequests, ApiResponse<AuthResponseDto>.Fail(result.Message));
            }
            if (result.Message?.Contains("not been verified") == true)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<AuthResponseDto>.Fail(result.Message));
            }
            return Unauthorized(ApiResponse<AuthResponseDto>.Fail(result.Message ?? "Giriş başarısız."));
        }

        // Set HttpOnly accessToken and refreshToken cookies
        SetAuthCookies(result.Data.AccessToken, result.Data.RefreshToken, command.RememberMe == true);

        // Sanitize response body: omit refreshToken from JSON while keeping HttpOnly cookie and in-memory accessToken
        var sanitizedData = new AuthResponseDto(
            result.Data.User,
            result.Data.AccessToken,
            result.Data.ExpiresIn,
            RefreshToken: null
        );

        return Ok(ApiResponse<AuthResponseDto>.Ok(
            sanitizedData,
            result.Message ?? "Giriş başarılı."
        ));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto? dto = null, CancellationToken cancellationToken = default)
    {
        var isSilentRefresh = Request.Headers.ContainsKey("X-Silent-Refresh");
        var rawRefreshToken = Request.Cookies["refreshToken"] ?? dto?.RefreshToken;
        if (string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            ClearAuthCookies();
            if (isSilentRefresh)
            {
                return Ok(ApiResponse<AuthResponseDto>.Fail("Refresh token not found."));
            }
            return Unauthorized(ApiResponse<AuthResponseDto>.Fail("Refresh token not found."));
        }

        var result = await Mediator.Send(new RefreshTokenCommand(rawRefreshToken), cancellationToken);
        if (!result.Success || result.Data == null)
        {
            ClearAuthCookies();
            if (isSilentRefresh)
            {
                return Ok(ApiResponse<AuthResponseDto>.Fail(result.Message ?? "Oturum süresi doldu veya geçersiz."));
            }
            return Unauthorized(ApiResponse<AuthResponseDto>.Fail(result.Message ?? "Oturum süresi doldu veya geçersiz."));
        }

        SetAuthCookies(result.Data.AccessToken, result.Data.RefreshToken, rememberMe: true);

        // Sanitize response body: omit refreshToken from JSON while keeping HttpOnly cookie and in-memory accessToken
        var sanitizedData = new AuthResponseDto(
            result.Data.User,
            result.Data.AccessToken,
            result.Data.ExpiresIn,
            RefreshToken: null
        );

        return Ok(ApiResponse<AuthResponseDto>.Ok(
            sanitizedData,
            result.Message ?? "Oturum yenilendi."
        ));
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        try
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                await Mediator.Send(new LogoutCommand(), cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Logout command exception, continuing to clear auth cookies.");
        }

        ClearAuthCookies();
        return Ok(ApiResponse<bool>.Ok(true, "Logged out successfully."));
    }

    [HttpPost("verify-email")]
    [AllowAnonymous]
    [EnableRateLimiting("AuthPolicy")]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailCommand command, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(command, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("resend-verification")]
    [AllowAnonymous]
    [EnableRateLimiting("AuthPolicy")]
    public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationCommand command, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(command, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting("AuthPolicy")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordCommand command, CancellationToken cancellationToken)
    {
        // Strictly use server-configured canonical FrontendBaseUrl to prevent Host / Origin header poisoning (DEV-SEC-004)
        var result = await Mediator.Send(command, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [EnableRateLimiting("AuthPolicy")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(command, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    private void SetAuthCookies(string accessToken, string? refreshToken, bool rememberMe = false)
    {
        var isHttps = Request.IsHttps;
        var sameSite = SameSiteMode.Lax;

        var accessCookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = isHttps,
            SameSite = sameSite,
            Path = "/"
        };

        if (rememberMe)
        {
            accessCookieOptions.Expires = DateTimeOffset.UtcNow.AddMinutes(60);
        }

        Response.Cookies.Append("accessToken", accessToken, accessCookieOptions);

        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            var refreshCookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = isHttps,
                SameSite = sameSite,
                Path = "/"
            };

            if (rememberMe)
            {
                refreshCookieOptions.Expires = DateTimeOffset.UtcNow.AddDays(30);
            }

            Response.Cookies.Append("refreshToken", refreshToken, refreshCookieOptions);
        }
    }

    private void ClearAuthCookies()
    {
        var isHttps = Request.IsHttps;
        var sameSite = SameSiteMode.Lax;

        var deleteOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = isHttps,
            SameSite = sameSite,
            Path = "/",
            Expires = DateTimeOffset.UtcNow.AddDays(-1)
        };

        Response.Cookies.Delete("accessToken", deleteOptions);
        Response.Cookies.Delete("refreshToken", deleteOptions);
        Response.Cookies.Delete("accessToken");
        Response.Cookies.Delete("refreshToken");
    }
}
