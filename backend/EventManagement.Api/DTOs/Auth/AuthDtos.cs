using System.ComponentModel.DataAnnotations;
using EventManagement.Api.Models;

namespace EventManagement.Api.DTOs.Auth;

public sealed record RegisterRequest(
    [param: Required, StringLength(150, MinimumLength = 2)] string Name,
    [param: Required, EmailAddress, StringLength(320)] string Email,
    [param: Required, StringLength(128, MinimumLength = 8)] string Password);

public sealed record LoginRequest(
    [param: Required, EmailAddress, StringLength(320)] string Email,
    [param: Required, StringLength(128)] string Password);

public sealed record ForgotPasswordRequest(
    [param: Required, EmailAddress, StringLength(320)] string Email);

public sealed record ResetPasswordRequest(
    [param: Required, StringLength(256)] string Token,
    [param: Required, StringLength(128, MinimumLength = 8)] string NewPassword);

public sealed record GoogleLoginRequest(
    [param: Required, StringLength(5000)] string IdToken);

public sealed record MessageResponse(string Message);

public sealed record UserResponse(
    Guid Id,
    string Name,
    string Email,
    UserRole Role,
    bool IsActive,
    DateTimeOffset CreatedAt,
    string? ImageUrl);

public sealed record AuthResponse(
    string Token,
    DateTimeOffset ExpiresAt,
    UserResponse User);
