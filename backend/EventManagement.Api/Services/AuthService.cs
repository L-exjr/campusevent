using EventManagement.Api.Data;
using EventManagement.Api.DTOs.Auth;
using EventManagement.Api.Infrastructure;
using EventManagement.Api.Mappings;
using EventManagement.Api.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace EventManagement.Api.Services;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<MessageResponse> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken);
    Task<MessageResponse> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken);
    Task<AuthResponse> GoogleLoginAsync(GoogleLoginRequest request, CancellationToken cancellationToken);
}

public sealed class AuthService(
    AppDbContext dbContext,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService,
    IGoogleTokenValidator googleTokenValidator,
    IEmailService emailService,
    IConfiguration configuration,
    ILogger<AuthService> logger) : IAuthService
{
    public const string ForgotPasswordMessage =
        "If an account exists for that email, a password reset link has been sent.";
    public async Task<AuthResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await dbContext.Users.AnyAsync(user => user.Email == email, cancellationToken))
            throw new ApiException(StatusCodes.Status409Conflict, "An account with this email already exists.");

        var user = new User
        {
            Name = request.Name.Trim(),
            Email = email,
            PasswordHash = passwordHasher.Hash(request.Password),
            AuthProvider = AuthProvider.Local,
            Role = UserRole.Student,
            IsActive = true
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);
        return CreateSession(user);
    }

    public async Task<MessageResponse> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await dbContext.Users.SingleOrDefaultAsync(
            item => item.Email == email && item.IsActive,
            cancellationToken);
        if (user is null) return new MessageResponse(ForgotPasswordMessage);

        var now = DateTimeOffset.UtcNow;
        var previousTokens = await dbContext.PasswordResetTokens
            .Where(token => token.UserId == user.Id && token.UsedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var previousToken in previousTokens) previousToken.UsedAt = now;

        var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        var resetToken = new PasswordResetToken
        {
            UserId = user.Id,
            User = user,
            TokenHash = HashResetToken(rawToken),
            ExpiresAt = now.AddMinutes(30)
        };
        dbContext.PasswordResetTokens.Add(resetToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var frontendBaseUrl = configuration["Frontend:BaseUrl"]?.TrimEnd('/')
            ?? "http://localhost:5173";
        var resetUrl = $"{frontendBaseUrl}/reset-password?token={Uri.EscapeDataString(rawToken)}";
        var sent = await emailService.SendAsync(
            user.Email,
            user.Name,
            "Reset your Campus Events password",
            "PasswordReset.html",
            new Dictionary<string, string?>
            {
                ["Name"] = user.Name,
                ["ResetUrl"] = resetUrl,
                ["ExpiresIn"] = "30 minutes"
            },
            cancellationToken);
        if (!sent)
            logger.LogError("Password reset token {TokenId} was created, but its email was not sent.",
                resetToken.Id);
        return new MessageResponse(ForgotPasswordMessage);
    }

    public async Task<MessageResponse> ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var tokenHash = HashResetToken(request.Token.Trim());
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var resetToken = await dbContext.PasswordResetTokens
            .FromSqlInterpolated($"SELECT * FROM \"PasswordResetTokens\" WHERE \"TokenHash\" = {tokenHash} FOR UPDATE")
            .Include(token => token.User)
            .SingleOrDefaultAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        if (resetToken is null || resetToken.UsedAt.HasValue || resetToken.ExpiresAt <= now)
            throw new ApiException(StatusCodes.Status400BadRequest, "The reset link is invalid or has expired.");

        resetToken.User.PasswordHash = passwordHasher.Hash(request.NewPassword);
        resetToken.User.AuthProvider = resetToken.User.AuthProvider == AuthProvider.Google
            ? AuthProvider.LocalAndGoogle
            : AuthProvider.Local;
        resetToken.UsedAt = now;
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new MessageResponse("Your password has been reset successfully.");
    }

    public async Task<AuthResponse> GoogleLoginAsync(
        GoogleLoginRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await googleTokenValidator.ValidateAsync(request.IdToken, cancellationToken);
        var email = identity.Email.Trim().ToLowerInvariant();
        var user = await dbContext.Users.SingleOrDefaultAsync(
            item => item.GoogleSubject == identity.Subject,
            cancellationToken);

        if (user is null)
        {
            user = await dbContext.Users.SingleOrDefaultAsync(item => item.Email == email, cancellationToken);
            if (user is null)
            {
                user = new User
                {
                    Name = identity.Name.Trim(),
                    Email = email,
                    PasswordHash = null,
                    AuthProvider = AuthProvider.Google,
                    GoogleSubject = identity.Subject,
                    ImageUrl = identity.PictureUrl,
                    Role = UserRole.Student,
                    IsActive = true
                };
                dbContext.Users.Add(user);
            }
            else
            {
                user.GoogleSubject = identity.Subject;
                user.AuthProvider = string.IsNullOrWhiteSpace(user.PasswordHash)
                    ? AuthProvider.Google
                    : AuthProvider.LocalAndGoogle;
            }
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
            throw new ApiException(StatusCodes.Status409Conflict, "This Google identity is already linked to another account.");
        if (!user.IsActive)
            throw new ApiException(StatusCodes.Status401Unauthorized, "This account is inactive.");
        return CreateSession(user);
    }

    private static string HashResetToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();

    public async Task<AuthResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await dbContext.Users.SingleOrDefaultAsync(
            item => item.Email == email,
            cancellationToken);
        var passwordResult = passwordHasher.Verify(request.Password, user?.PasswordHash);
        if (user is null || passwordResult == PasswordVerificationResult.Failed)
            throw new ApiException(StatusCodes.Status401Unauthorized, "The email or password is incorrect.");
        if (!user.IsActive)
            throw new ApiException(StatusCodes.Status401Unauthorized, "This account is inactive.");

        if (passwordResult == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = passwordHasher.Hash(request.Password);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return CreateSession(user);
    }

    private AuthResponse CreateSession(User user)
    {
        var token = jwtTokenService.Create(user);
        return new AuthResponse(token.Token, token.ExpiresAt, user.ToResponse());
    }
}
