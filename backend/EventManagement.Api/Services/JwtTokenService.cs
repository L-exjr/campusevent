using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EventManagement.Api.Infrastructure;
using EventManagement.Api.Models;
using Microsoft.IdentityModel.Tokens;

namespace EventManagement.Api.Services;

public sealed record TokenResult(string Token, DateTimeOffset ExpiresAt);

public interface IJwtTokenService
{
    TokenResult Create(User user);
}

public sealed class JwtTokenService(IConfiguration configuration) : IJwtTokenService
{
    public TokenResult Create(User user)
    {
        var key = configuration["Jwt:SigningKey"]
            ?? throw new InvalidOperationException("Jwt:SigningKey is required.");
        var issuer = configuration["Jwt:Issuer"] ?? "EventManagement.Api";
        var audience = configuration["Jwt:Audience"] ?? "EventManagement.Frontend";
        var expiryMinutes = Math.Clamp(configuration.GetValue("Jwt:ExpiryMinutes", 75), 60, 90);
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(expiryMinutes);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtClaimNames.UserId, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Name, user.Name),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtClaimNames.Role, user.Role.ToString()),
            new Claim(JwtClaimNames.SessionVersion, user.SessionVersion.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);
        return new TokenResult(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
