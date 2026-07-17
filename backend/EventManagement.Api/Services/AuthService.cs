using EventManagement.Api.Data;
using EventManagement.Api.DTOs.Auth;
using EventManagement.Api.Infrastructure;
using EventManagement.Api.Mappings;
using EventManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Api.Services;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
}

public sealed class AuthService(
    AppDbContext dbContext,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService) : IAuthService
{
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
            Role = UserRole.Student,
            IsActive = true
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);
        return CreateSession(user);
    }

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
