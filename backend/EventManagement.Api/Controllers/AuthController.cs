using EventManagement.Api.DTOs.Auth;
using EventManagement.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EventManagement.Api.Infrastructure;
using System.Security.Cryptography;

namespace EventManagement.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    IAuthService authService,
    IUserService userService,
    IConfiguration configuration,
    IWebHostEnvironment environment) : ControllerBase
{
    private readonly bool secureCookies = configuration.GetValue<bool?>("Security:SecureCookies")
        ?? !environment.IsDevelopment();

    [AllowAnonymous]
    [HttpGet("csrf")]
    public ActionResult<object> Csrf()
    {
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        Response.Cookies.Append("campus_events_csrf", token, new CookieOptions {
            HttpOnly = false,
            Secure = secureCookies,
            SameSite = secureCookies ? SameSiteMode.None : SameSiteMode.Lax,
            Path = "/",
            IsEssential = true
        });
        return Ok(new { token });
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var session = await authService.RegisterAsync(request, cancellationToken);
        IssueCookie(session);
        return StatusCode(StatusCodes.Status201Created, session);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var session = await authService.LoginAsync(request, cancellationToken);
        IssueCookie(session);
        return Ok(session);
    }

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    public async Task<ActionResult<MessageResponse>> ForgotPassword(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken) =>
        Ok(await authService.ForgotPasswordAsync(request, cancellationToken));

    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<ActionResult<MessageResponse>> ResetPassword(
        ResetPasswordRequest request,
        CancellationToken cancellationToken) =>
        Ok(await authService.ResetPasswordAsync(request, cancellationToken));

    [AllowAnonymous]
    [HttpPost("google")]
    public async Task<ActionResult<AuthResponse>> GoogleLogin(
        GoogleLoginRequest request,
        CancellationToken cancellationToken)
    {
        var session = await authService.GoogleLoginAsync(request, cancellationToken);
        IssueCookie(session);
        return Ok(session);
    }

    [Authorize]
    [HttpGet("session")]
    public async Task<ActionResult<UserResponse>> Session(CancellationToken cancellationToken) =>
        Ok(await userService.GetByIdAsync(User.GetRequiredUserId(), cancellationToken));

    [Authorize]
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete(AuthCookie.Name, AuthCookie.Options(DateTimeOffset.UnixEpoch, secureCookies));
        return NoContent();
    }

    private void IssueCookie(AuthResponse session)
        => Response.Cookies.Append(AuthCookie.Name, session.Token, AuthCookie.Options(session.ExpiresAt, secureCookies));
}
