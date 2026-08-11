using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EventManagement.Api.Infrastructure;
using Microsoft.IdentityModel.Tokens;

namespace EventManagement.Api.Services;

public sealed record TicketTokenClaims(
    Guid RegistrationId,
    Guid EventId,
    Guid StudentId);

public interface ITicketTokenService
{
    string Create(
        Guid registrationId,
        Guid eventId,
        Guid studentId,
        DateTimeOffset expiresAt);
    TicketTokenClaims Validate(string token);
}

public sealed class TicketTokenService(IConfiguration configuration) : ITicketTokenService
{
    private const string Issuer = "EventManagement.Api";
    private const string Audience = "EventManagement.CheckIn";

    public string Create(
        Guid registrationId,
        Guid eventId,
        Guid studentId,
        DateTimeOffset expiresAt)
    {
        var credentials = new SigningCredentials(GetKey(), SecurityAlgorithms.HmacSha256);
        return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            Issuer,
            Audience,
            [
                new Claim(JwtRegisteredClaimNames.Sub, studentId.ToString()),
                new Claim("registration_id", registrationId.ToString()),
                new Claim("event_id", eventId.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
            ],
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials));
    }

    public TicketTokenClaims Validate(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ApiException(StatusCodes.Status400BadRequest, "The ticket token is required.");
        try
        {
            var principal = new JwtSecurityTokenHandler { MapInboundClaims = false }.ValidateToken(
                token,
                new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = Issuer,
                    ValidateAudience = true,
                    ValidAudience = Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = GetKey(),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = JwtRegisteredClaimNames.Sub
                },
                out _);
            return new TicketTokenClaims(
                ReadGuid(principal, "registration_id"),
                ReadGuid(principal, "event_id"),
                ReadGuid(principal, JwtRegisteredClaimNames.Sub));
        }
        catch (ApiException)
        {
            throw;
        }
        catch (Exception exception) when (exception is SecurityTokenException or ArgumentException)
        {
            throw new ApiException(StatusCodes.Status400BadRequest, "The ticket is invalid or expired.");
        }
    }

    private SymmetricSecurityKey GetKey()
    {
        var signingKey = configuration["QR_SIGNING_KEY"];
        if (string.IsNullOrWhiteSpace(signingKey))
            signingKey = configuration["Tickets:SigningKey"];
        if (string.IsNullOrWhiteSpace(signingKey) || signingKey.Length < 32)
            throw new ApiException(
                StatusCodes.Status503ServiceUnavailable,
                "Ticket check-in is not configured.");
        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
    }

    private static Guid ReadGuid(ClaimsPrincipal principal, string claimName) =>
        Guid.TryParse(principal.FindFirstValue(claimName), out var value)
            ? value
            : throw new ApiException(StatusCodes.Status400BadRequest, "The ticket is invalid.");
}
