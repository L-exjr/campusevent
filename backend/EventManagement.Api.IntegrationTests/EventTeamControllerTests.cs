using System.Net;
using System.Net.Http.Json;

namespace EventManagement.Api.IntegrationTests;

public sealed class EventTeamControllerTests(ApiIntegrationFixture fixture)
    : IntegrationTestBase(fixture), IClassFixture<ApiIntegrationFixture>
{
    [Fact]
    public async Task Owner_and_each_team_role_enforce_their_actual_endpoint_boundaries()
    {
        await ResetAsync();
        var owner = await RegisterStudentAsync("team-owner@example.test");
        var teamAdmin = await RegisterStudentAsync("team-admin@example.test");
        var member = await RegisterStudentAsync("team-member@example.test");
        var checkIn = await RegisterStudentAsync("team-checkin@example.test");
        var attendee = await RegisterStudentAsync("team-attendee@example.test");
        var eventId = await CreateEventAsync(owner.Token, "Collaborative event", 20);
        await RegisterForEventAsync(attendee.Token, eventId);
        using var ownerClient = CreateAuthenticatedClient(owner.Token);
        await InviteAsync(ownerClient, eventId, teamAdmin, "team-admin@example.test", "Admin");
        await InviteAsync(ownerClient, eventId, member, "team-member@example.test", "Member");
        await InviteAsync(ownerClient, eventId, checkIn, "team-checkin@example.test", "CheckInStaff");

        using var adminClient = CreateAuthenticatedClient(teamAdmin.Token);
        Assert.Equal(HttpStatusCode.OK, (await adminClient.GetAsync($"/api/events/{eventId}/team")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await adminClient.GetAsync($"/api/events/{eventId}/revenue")).StatusCode);

        using var memberClient = CreateAuthenticatedClient(member.Token);
        Assert.Equal(HttpStatusCode.OK, (await memberClient.PutAsJsonAsync($"/api/events/{eventId}", UpdatePayload(1))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await memberClient.GetAsync($"/api/events/{eventId}/revenue")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await memberClient.GetAsync($"/api/events/{eventId}/team")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await memberClient.DeleteAsync($"/api/events/{eventId}")).StatusCode);

        using var checkInClient = CreateAuthenticatedClient(checkIn.Token);
        Assert.Equal(HttpStatusCode.OK, (await checkInClient.GetAsync($"/api/events/{eventId}/registrants")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await checkInClient.PutAsJsonAsync($"/api/events/{eventId}/attendance",
            new { registrations = new[] { new { registrationId = await RegistrationIdAsync(ownerClient, eventId), attended = true } } })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await checkInClient.PutAsJsonAsync($"/api/events/{eventId}", UpdatePayload(2))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await checkInClient.GetAsync($"/api/events/{eventId}/revenue")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await checkInClient.GetAsync($"/api/events/{eventId}/team")).StatusCode);

        Assert.Equal(HttpStatusCode.Conflict,
            (await ownerClient.DeleteAsync($"/api/events/{eventId}/team/{owner.UserId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,
            (await ownerClient.DeleteAsync($"/api/events/{eventId}/team/{checkIn.UserId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await adminClient.DeleteAsync($"/api/events/{eventId}")).StatusCode);
    }

    [Fact]
    public async Task Invitation_requires_an_existing_active_account()
    {
        await ResetAsync();
        var owner = await RegisterStudentAsync("invite-owner@example.test");
        var eventId = await CreateEventAsync(owner.Token, "Invitation event", 10);
        using var client = CreateAuthenticatedClient(owner.Token);
        using var response = await client.PostAsJsonAsync($"/api/events/{eventId}/team",
            new { email = "missing@example.test", role = "Member" });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("create an account", (await ReadJsonAsync(response)).GetProperty("error").GetString());
    }

    private static async Task InviteAsync(HttpClient client, Guid eventId, TestSession user, string email, string role)
    {
        using var response = await client.PostAsJsonAsync($"/api/events/{eventId}/team", new { email, role });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(user.UserId, (await ReadJsonAsync(response)).GetProperty("userId").GetGuid());
    }

    private static object UpdatePayload(int version) => new
    {
        title = "Collaborative event updated", description = "A sufficiently detailed collaborative event description.",
        date = DateTimeOffset.UtcNow.AddDays(7), location = "Integration Test Hall", capacity = 20,
        category = "Startup & Tech", priceMinor = 0, currency = "GHS", version
    };

    private static async Task<Guid> RegistrationIdAsync(HttpClient ownerClient, Guid eventId)
    {
        using var response = await ownerClient.GetAsync($"/api/events/{eventId}/registrants");
        response.EnsureSuccessStatusCode();
        return (await ReadJsonAsync(response)).GetProperty("items")[0].GetProperty("registrationId").GetGuid();
    }
}
