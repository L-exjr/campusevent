using System.Net;

namespace EventManagement.Api.IntegrationTests;

public sealed class ReportsControllerTests(ApiIntegrationFixture fixture)
    : IntegrationTestBase(fixture), IClassFixture<ApiIntegrationFixture>
{
    [Fact]
    public async Task Event_reports_are_aggregated_and_paginated_in_one_endpoint()
    {
        await ResetAsync();
        var organizer = await CreateActorAsync("report-organizer@example.test", "Organizer");
        var attendee = await RegisterStudentAsync("report-attendee@example.test");
        var firstEvent = await CreateEventAsync(organizer.Token, "First aggregate report event", 10);
        await CreateEventAsync(organizer.Token, "Second aggregate report event", 10);
        await RegisterForEventAsync(attendee.Token, firstEvent);
        var admin = await LoginAdminAsync();
        using var client = CreateAuthenticatedClient(admin.Token);

        using var response = await client.GetAsync("/api/reports/events?page=1&pageSize=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadJsonAsync(response);
        Assert.Equal(1, body.GetProperty("page").GetInt32());
        Assert.Equal(1, body.GetProperty("pageSize").GetInt32());
        Assert.Equal(2, body.GetProperty("totalCount").GetInt32());
        Assert.Equal(2, body.GetProperty("totalPages").GetInt32());
        var item = body.GetProperty("items").EnumerateArray().Single();
        Assert.Equal(organizer.UserId, item.GetProperty("organizerId").GetGuid());
        Assert.Equal("report-organizer", item.GetProperty("organizerName").GetString());
        Assert.True(item.TryGetProperty("registrationCount", out _));
        Assert.True(item.TryGetProperty("attendanceCount", out _));
        Assert.True(item.TryGetProperty("attendanceRate", out _));
    }

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
        await Fixture.SetEventDateAsync(eventId, DateTimeOffset.UtcNow.AddDays(-1));
        await SetRoleAsync(admin.Token, candidate.UserId, "Student");
        using var client = CreateAuthenticatedClient(admin.Token);

        using var response = await client.GetAsync("/api/reports/organizers");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var report = await ReadJsonAsync(response);
        var item = report.GetProperty("items").EnumerateArray().Single(entry =>
            entry.GetProperty("organizerId").GetGuid() == candidate.UserId);
        Assert.Equal(1, item.GetProperty("eventCount").GetInt32());
        Assert.Equal(1, item.GetProperty("registrationCount").GetInt32());
    }
}
