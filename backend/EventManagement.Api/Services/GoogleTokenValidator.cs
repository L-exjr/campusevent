using Google.Apis.Auth;

namespace EventManagement.Api.Services;

public sealed record GoogleIdentity(string Subject, string Email, string Name, string? PictureUrl);

public interface IGoogleTokenValidator
{
    Task<GoogleIdentity> ValidateAsync(string idToken, CancellationToken cancellationToken);
}

public sealed class GoogleTokenValidator(IConfiguration configuration) : IGoogleTokenValidator
{
    public async Task<GoogleIdentity> ValidateAsync(
        string idToken,
        CancellationToken cancellationToken)
    {
        var clientId = configuration["Google:ClientId"];
        if (string.IsNullOrWhiteSpace(clientId))
            throw new InvalidOperationException("Google:ClientId is not configured.");

        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(
                idToken,
                new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = [clientId]
                });
        }
        catch (Exception exception) when (exception is InvalidJwtException or FormatException or ArgumentException)
        {
            throw new Infrastructure.ApiException(
                StatusCodes.Status401Unauthorized,
                "The Google sign-in token is invalid or expired.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(payload.Subject) ||
            string.IsNullOrWhiteSpace(payload.Email) ||
            payload.EmailVerified != true)
        {
            throw new Infrastructure.ApiException(
                StatusCodes.Status401Unauthorized,
                "Google could not verify this account's email address.");
        }

        return new GoogleIdentity(
            payload.Subject,
            payload.Email,
            string.IsNullOrWhiteSpace(payload.Name) ? payload.Email.Split('@')[0] : payload.Name,
            payload.Picture);
    }
}
