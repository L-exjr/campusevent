using EventManagement.Api.Services;
using Microsoft.Extensions.Configuration;

namespace EventManagement.Api.UnitTests.Services;

public sealed class TicketTokenServiceTests
{
    [Fact]
    public void Created_ticket_round_trips_signed_claims()
    {
        var service = CreateService();
        var registrationId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var studentId = Guid.NewGuid();

        var token = service.Create(
            registrationId,
            eventId,
            studentId,
            DateTimeOffset.UtcNow.AddHours(1));
        var claims = service.Validate(token);

        Assert.Equal(registrationId, claims.RegistrationId);
        Assert.Equal(eventId, claims.EventId);
        Assert.Equal(studentId, claims.StudentId);
    }

    [Fact]
    public void Tampered_ticket_is_rejected()
    {
        var service = CreateService();
        var token = service.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddHours(1));

        Assert.Throws<EventManagement.Api.Infrastructure.ApiException>(() =>
            service.Validate(token[..^1] + (token[^1] == 'a' ? 'b' : 'a')));
    }

    private static TicketTokenService CreateService() => new(
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Tickets:SigningKey"] = "unit-test-ticket-signing-key-that-is-long-enough"
            })
            .Build());
}
