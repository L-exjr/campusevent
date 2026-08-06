using System.IdentityModel.Tokens.Jwt;
using EventManagement.Api.Models;
using EventManagement.Api.Services;
using Microsoft.Extensions.Configuration;

namespace EventManagement.Api.UnitTests.Services;

public sealed class JwtTokenServiceTests
{
    [Fact]
    public void Uses_injected_time_provider_for_expiry()
    {
        var now = new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.Zero);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["Jwt:SigningKey"] = new string('x', 64),
                ["Jwt:ExpiryMinutes"] = "75"
            }).Build();
        var service = new JwtTokenService(configuration, new FixedTimeProvider(now));

        var result = service.Create(new User
        {
            Name = "Clock Test",
            Email = "clock@example.test",
            Role = UserRole.Student
        });

        Assert.Equal(now.AddMinutes(75), result.ExpiresAt);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);
        Assert.Equal(result.ExpiresAt.ToUnixTimeSeconds(), jwt.Payload.Expiration);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
