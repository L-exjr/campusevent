using System.Net;

namespace EventManagement.Api.IntegrationTests;

public sealed class ReportsControllerTests(ApiIntegrationFixture fixture)
    : IntegrationTestBase(fixture), IClassFixture<ApiIntegrationFixture>
{
    [Fact]
    public async Task Demoted_organizer_with_owned_events_remains_in_historical_report()
    {
        await ResetAsync();
        var admin = await LoginAdminAsync();
        var candidate = await RegisterStudentAsync("former-organizer@example.test");
        var attendee = await RegisterStudentAsync("former-organizer-attendee@example.test");
        await SetRoleAsync(admin.Token, candidate.UserId, "Organizer");
        var organizer = await LoginAsync("former-organizer@example.test");
        var eventId = await CreateEventAsync(organizer.Token, "Historical report event", 10);
        await RegisterForEventAsync(attendee.Token, eventId);
        await SetRoleAsync(admin.Token, candidate.UserId, "Student");
        using var client = CreateAuthenticatedClient(admin.Token);

        using var response = await client.GetAsync("/api/reports/organizers");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var report = await ReadJsonAsync(response);
        var item = report.EnumerateArray().Single(entry =>
            entry.GetProperty("organizerId").GetGuid() == candidate.UserId);
        Assert.Equal(1, item.GetProperty("eventCount").GetInt32());
        Assert.Equal(1, item.GetProperty("registrationCount").GetInt32());
    }
}
