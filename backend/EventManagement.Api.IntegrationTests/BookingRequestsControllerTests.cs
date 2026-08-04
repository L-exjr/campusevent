using System.Net;
using System.Net.Http.Json;

namespace EventManagement.Api.IntegrationTests;

public sealed class BookingRequestsControllerTests(ApiIntegrationFixture fixture)
    : IntegrationTestBase(fixture), IClassFixture<ApiIntegrationFixture>
{
    [Fact]
    public async Task Honeypot_submission_is_accepted_but_not_stored()
    {
        await ResetAsync();
        using var client = Fixture.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/booking-requests", Payload("bot.example"));
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal(0, await Fixture.CountBookingRequestsAsync());
    }

    [Fact]
    public async Task Admin_assigns_and_assigned_organizer_accepts_creating_an_unpublished_draft()
    {
        await ResetAsync();
        var organizer = await CreateActorAsync("booking-organizer@example.test", "Organizer");
        var admin = await LoginAdminAsync();
        using var publicClient = Fixture.CreateClient();
        using var submission = await publicClient.PostAsJsonAsync("/api/booking-requests", Payload());
        var bookingId = (await ReadJsonAsync(submission)).GetProperty("id").GetGuid();

        using var adminClient = CreateAuthenticatedClient(admin.Token);
        using var assignment = await adminClient.PutAsJsonAsync(
            $"/api/booking-requests/{bookingId}/assign", new { organizerId = organizer.UserId });
        Assert.Equal(HttpStatusCode.OK, assignment.StatusCode);

        using var organizerClient = CreateAuthenticatedClient(organizer.Token);
        using var response = await organizerClient.PutAsJsonAsync(
            $"/api/booking-requests/{bookingId}/respond", new { accept = true, note = "Happy to help." });
        response.EnsureSuccessStatusCode();
        var draftId = (await ReadJsonAsync(response)).GetProperty("draftEventId").GetGuid();
        var state = await Fixture.GetEventStateAsync(draftId);
        Assert.False(state.IsPublished);
        Assert.Equal(organizer.UserId, state.OrganizerId);
        using var publicEvent = await publicClient.GetAsync($"/api/events/{draftId}");
        Assert.Equal(HttpStatusCode.NotFound, publicEvent.StatusCode);
        using var adminEvents = await adminClient.GetAsync("/api/events/all");
        adminEvents.EnsureSuccessStatusCode();
        Assert.Contains(
            (await ReadJsonAsync(adminEvents)).GetProperty("items").EnumerateArray(),
            item => item.GetProperty("id").GetGuid() == draftId &&
                    !item.GetProperty("isPublished").GetBoolean());
    }

    [Fact]
    public async Task Unassigned_organizer_cannot_respond()
    {
        await ResetAsync();
        var organizer = await CreateActorAsync("unassigned-organizer@example.test", "Organizer");
        using var publicClient = Fixture.CreateClient();
        using var submission = await publicClient.PostAsJsonAsync("/api/booking-requests", Payload());
        var bookingId = (await ReadJsonAsync(submission)).GetProperty("id").GetGuid();
        using var organizerClient = CreateAuthenticatedClient(organizer.Token);
        using var response = await organizerClient.PutAsJsonAsync(
            $"/api/booking-requests/{bookingId}/respond", new { accept = false, note = "No" });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Two_simultaneous_accepts_create_one_draft_and_one_conflict()
    {
        await ResetAsync();
        var organizer = await CreateActorAsync("concurrent-booking-organizer@example.test", "Organizer");
        var admin = await LoginAdminAsync();
        using var publicClient = Fixture.CreateClient();
        using var submission = await publicClient.PostAsJsonAsync("/api/booking-requests", Payload());
        var bookingId = (await ReadJsonAsync(submission)).GetProperty("id").GetGuid();

        using var adminClient = CreateAuthenticatedClient(admin.Token);
        using var assignment = await adminClient.PutAsJsonAsync(
            $"/api/booking-requests/{bookingId}/assign", new { organizerId = organizer.UserId });
        assignment.EnsureSuccessStatusCode();

        using var firstClient = CreateAuthenticatedClient(organizer.Token);
        using var secondClient = CreateAuthenticatedClient(organizer.Token);
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstTask = SendAfterGateAsync(
            gate.Task,
            () => firstClient.PutAsJsonAsync(
                $"/api/booking-requests/{bookingId}/respond",
                new { accept = true, note = "First concurrent response." }));
        var secondTask = SendAfterGateAsync(
            gate.Task,
            () => secondClient.PutAsJsonAsync(
                $"/api/booking-requests/{bookingId}/respond",
                new { accept = true, note = "Second concurrent response." }));

        gate.SetResult();
        var responses = await Task.WhenAll(firstTask, secondTask);
        try
        {
            Assert.Equal(1, responses.Count(response => response.StatusCode == HttpStatusCode.OK));
            Assert.Equal(1, responses.Count(response => response.StatusCode == HttpStatusCode.Conflict));
            Assert.Equal(1, await Fixture.CountEventsAsync());
        }
        finally
        {
            foreach (var response in responses) response.Dispose();
        }
    }

    private static object Payload(string website = "") => new
    {
        organizationName = "Integration Test Society",
        contactName = "Casey Contact",
        email = "casey@example.test",
        phone = "+233 20 000 0000",
        eventType = "Leadership workshop",
        proposedDate = DateTimeOffset.UtcNow.AddDays(21),
        alternativeDates = "Any weekday that week",
        flexibilityNote = "Afternoons preferred",
        estimatedAttendance = 80,
        preferredOrganizer = "",
        description = "A detailed public booking request for an integration test event.",
        website
    };
}

public sealed class BookingRequestRateLimitTests(ApiIntegrationFixture fixture)
    : IntegrationTestBase(fixture), IClassFixture<ApiIntegrationFixture>
{
    [Fact]
    public async Task Public_submission_is_limited_to_five_requests_per_IP_per_hour()
    {
        await ResetAsync();
        using var client = Fixture.CreateClient();
        for (var index = 0; index < 5; index++)
        {
            using var allowed = await client.PostAsJsonAsync("/api/booking-requests", Payload(index));
            Assert.Equal(HttpStatusCode.Accepted, allowed.StatusCode);
        }

        using var blocked = await client.PostAsJsonAsync("/api/booking-requests", Payload(6));
        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
    }

    private static object Payload(int index) => new
    {
        organizationName = $"Rate Limit Society {index}",
        contactName = "Rate Limit Contact",
        email = $"rate-limit-{index}@example.test",
        phone = "+233 20 000 0000",
        eventType = "Public workshop",
        proposedDate = DateTimeOffset.UtcNow.AddDays(30),
        estimatedAttendance = 25,
        description = "A valid request used to verify the fixed-window public endpoint limit.",
        website = ""
    };
}
