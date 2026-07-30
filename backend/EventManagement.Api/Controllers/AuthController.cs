using EventManagement.Api.DTOs.Auth;
using EventManagement.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventManagement.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var user = await authService.RegisterAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, user);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken) =>
        Ok(await authService.LoginAsync(request, cancellationToken));

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
        CancellationToken cancellationToken) =>
        Ok(await authService.GoogleLoginAsync(request, cancellationToken));
}
