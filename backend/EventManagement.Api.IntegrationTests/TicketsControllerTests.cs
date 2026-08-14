using System.Net;
using System.Net.Http.Json;

namespace EventManagement.Api.IntegrationTests;

public sealed class TicketsControllerTests(ApiIntegrationFixture fixture)
    : IntegrationTestBase(fixture), IClassFixture<ApiIntegrationFixture>
{
    [Fact]
    public async Task Organizer_can_check_in_signed_ticket_only_once()
    {
        await ResetAsync();
        var organizer = await CreateActorAsync("ticket-owner@example.test", "Organizer");
        var student = await RegisterStudentAsync("ticket-student@example.test");
        var eventId = await CreateEventAsync(organizer.Token, "Ticketed event", 10);
        var registrationId = await RegisterForEventAsync(student.Token, eventId);
        var token = await GetTicketTokenAsync(student.Token, registrationId);
        using var client = CreateAuthenticatedClient(organizer.Token);

        using var first = await client.PostAsJsonAsync(
            $"/api/events/{eventId}/check-in",
            new { token });
        using var duplicate = await client.PostAsJsonAsync(
            $"/api/events/{eventId}/check-in",
            new { token });

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        using var registrants = await client.GetAsync($"/api/events/{eventId}/registrants");
        registrants.EnsureSuccessStatusCode();
        Assert.True((await ReadJsonAsync(registrants))
            .GetProperty("items")[0]
            .GetProperty("attended")
            .GetBoolean());
    }

    [Fact]
    public async Task Ticket_cannot_be_used_by_another_event_organizer()
    {
        await ResetAsync();
        var owner = await CreateActorAsync("ticket-real-owner@example.test", "Organizer");
        var other = await CreateActorAsync("ticket-other-owner@example.test", "Organizer");
        var student = await RegisterStudentAsync("ticket-private@example.test");
        var eventId = await CreateEventAsync(owner.Token, "Private ticket event", 10);
        var registrationId = await RegisterForEventAsync(student.Token, eventId);
        var token = await GetTicketTokenAsync(student.Token, registrationId);
        using var client = CreateAuthenticatedClient(other.Token);

        using var response = await client.PostAsJsonAsync(
            $"/api/events/{eventId}/check-in",
            new { token });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Event_owner_can_check_in_with_short_ticket_code_and_non_owner_cannot()
    {
        await ResetAsync();
        var owner = await CreateActorAsync("code-owner@example.test", "Organizer");
        var other = await CreateActorAsync("code-other@example.test", "Organizer");
        var student = await RegisterStudentAsync("code-student@example.test");
        var eventId = await CreateEventAsync(owner.Token, "Manual code event", 10);
        await RegisterForEventAsync(student.Token, eventId);
        var code = await Fixture.GetTicketCodeAsync(eventId, student.UserId);
        Assert.StartsWith("EMS-", code);
        using var otherClient = CreateAuthenticatedClient(other.Token);
        using var forbidden = await otherClient.PostAsJsonAsync(
            $"/api/events/{eventId}/check-in/manual", new { ticketCode = code });
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        using var ownerClient = CreateAuthenticatedClient(owner.Token);
        using var checkedIn = await ownerClient.PostAsJsonAsync(
            $"/api/events/{eventId}/check-in/manual", new { ticketCode = code.ToLowerInvariant() });
        Assert.Equal(HttpStatusCode.OK, checkedIn.StatusCode);
    }

    [Fact]
    public async Task Tampered_ticket_is_rejected()
    {
        await ResetAsync();
        var organizer = await CreateActorAsync("ticket-tamper-owner@example.test", "Organizer");
        var student = await RegisterStudentAsync("ticket-tamper-student@example.test");
        var eventId = await CreateEventAsync(organizer.Token, "Tamper proof event", 10);
        var registrationId = await RegisterForEventAsync(student.Token, eventId);
        var token = await GetTicketTokenAsync(student.Token, registrationId);
        using var client = CreateAuthenticatedClient(organizer.Token);

        using var response = await client.PostAsJsonAsync(
            $"/api/events/{eventId}/check-in",
            new { token = token[..^1] + (token[^1] == 'a' ? 'b' : 'a') });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<string> GetTicketTokenAsync(string studentToken, Guid registrationId)
    {
        using var client = CreateAuthenticatedClient(studentToken);
        using var response = await client.GetAsync($"/api/tickets/{registrationId}");
        response.EnsureSuccessStatusCode();
        return (await ReadJsonAsync(response)).GetProperty("token").GetString()!;
    }
}
