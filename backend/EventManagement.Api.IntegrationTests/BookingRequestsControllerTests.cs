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
        using var client = CookieClient();
        SetClientAddress(client, "203.0.113.20");
        using var response = await SendWithCsrfAsync(client, HttpMethod.Post, "/api/booking-requests", Payload("bot.example"));
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal(0, await Fixture.CountBookingRequestsAsync());
    }

    [Fact]
    public async Task Admin_assigns_and_assigned_organizer_accepts_creating_an_unpublished_draft()
    {
        await ResetAsync();
        var organizer = await CreateActorAsync("booking-organizer@example.test", "Organizer");
        var admin = await LoginAdminAsync();
        using var publicClient = CookieClient();
        SetClientAddress(publicClient, "203.0.113.21");
        using var submission = await SendWithCsrfAsync(publicClient, HttpMethod.Post, "/api/booking-requests", Payload());
        var bookingId = (await ReadJsonAsync(submission)).GetProperty("id").GetGuid();

        using var adminClient = CreateAuthenticatedClient(admin.Token);
        using var assignment = await adminClient.PutAsJsonAsync(
            $"/api/booking-requests/{bookingId}/assign", new { organizerId = organizer.UserId });
        Assert.True(assignment.IsSuccessStatusCode, await assignment.Content.ReadAsStringAsync());

        using var organizerClient = CreateAuthenticatedClient(organizer.Token);
        using var quote = await SubmitQuoteAsync(organizerClient, bookingId);
        quote.EnsureSuccessStatusCode();
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
    public async Task Admin_can_list_submitted_booking_requests()
    {
        await ResetAsync();
        using var publicClient = CookieClient();
        SetClientAddress(publicClient, "203.0.113.27");
        using var submission = await SendWithCsrfAsync(publicClient, HttpMethod.Post, "/api/booking-requests", Payload());
        submission.EnsureSuccessStatusCode();
        var bookingId = (await ReadJsonAsync(submission)).GetProperty("id").GetGuid();
        var admin = await LoginAdminAsync();
        using var adminClient = CreateAuthenticatedClient(admin.Token);

        using var response = await adminClient.GetAsync("/api/booking-requests?page=1&pageSize=20");

        response.EnsureSuccessStatusCode();
        var body = await ReadJsonAsync(response);
        Assert.Equal(1, body.GetProperty("totalCount").GetInt32());
        Assert.Equal(bookingId, body.GetProperty("items")[0].GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task Tracking_token_is_scoped_to_one_request_and_returns_history()
    {
        await ResetAsync();
        using var client = CookieClient();
        SetClientAddress(client, "203.0.113.29");
        using var submission = await SendWithCsrfAsync(client, HttpMethod.Post, "/api/booking-requests", Payload());
        submission.EnsureSuccessStatusCode();
        var body = await ReadJsonAsync(submission);
        var id = body.GetProperty("id").GetGuid();
        var token = body.GetProperty("trackingToken").GetString();

        using var tracked = await client.GetAsync($"/api/booking-requests/{id}/track?token={Uri.EscapeDataString(token!)}");
        tracked.EnsureSuccessStatusCode();
        var trackedBody = await ReadJsonAsync(tracked);
        Assert.Equal("Submitted", trackedBody.GetProperty("status").GetString());
        Assert.Single(trackedBody.GetProperty("statusHistory").EnumerateArray());

        using var rejected = await client.GetAsync($"/api/booking-requests/{id}/track?token=wrong-token");
        Assert.Equal(HttpStatusCode.NotFound, rejected.StatusCode);
    }

    [Fact]
    public async Task Unassigned_organizer_cannot_respond()
    {
        await ResetAsync();
        var organizer = await CreateActorAsync("unassigned-organizer@example.test", "Organizer");
        using var publicClient = CookieClient();
        SetClientAddress(publicClient, "203.0.113.22");
        using var submission = await SendWithCsrfAsync(publicClient, HttpMethod.Post, "/api/booking-requests", Payload());
        var bookingId = (await ReadJsonAsync(submission)).GetProperty("id").GetGuid();
        using var organizerClient = CreateAuthenticatedClient(organizer.Token);
        using var response = await organizerClient.PutAsJsonAsync(
            $"/api/booking-requests/{bookingId}/respond", new { accept = false, note = "No" });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Closed_request_cannot_return_to_under_review()
    {
        await ResetAsync();
        using var publicClient = CookieClient();
        SetClientAddress(publicClient, "203.0.113.23");
        using var submission = await SendWithCsrfAsync(publicClient, HttpMethod.Post, "/api/booking-requests", Payload());
        var bookingId = (await ReadJsonAsync(submission)).GetProperty("id").GetGuid();
        var admin = await LoginAdminAsync();
        using var client = CreateAuthenticatedClient(admin.Token);
        using var closed = await client.PutAsJsonAsync(
            $"/api/booking-requests/{bookingId}/status",
            new { status = "Closed" });
        Assert.True(closed.IsSuccessStatusCode, await closed.Content.ReadAsStringAsync());

        using var invalid = await client.PutAsJsonAsync(
            $"/api/booking-requests/{bookingId}/status",
            new { status = "UnderReview" });

        Assert.Equal(HttpStatusCode.Conflict, invalid.StatusCode);
    }

    [Fact]
    public async Task Two_simultaneous_accepts_create_one_draft_and_one_conflict()
    {
        await ResetAsync();
        var organizer = await CreateActorAsync("concurrent-booking-organizer@example.test", "Organizer");
        var admin = await LoginAdminAsync();
        using var publicClient = CookieClient();
        SetClientAddress(publicClient, "203.0.113.24");
        using var submission = await SendWithCsrfAsync(publicClient, HttpMethod.Post, "/api/booking-requests", Payload());
        var bookingId = (await ReadJsonAsync(submission)).GetProperty("id").GetGuid();

        using var adminClient = CreateAuthenticatedClient(admin.Token);
        using var assignment = await adminClient.PutAsJsonAsync(
            $"/api/booking-requests/{bookingId}/assign", new { organizerId = organizer.UserId });
        assignment.EnsureSuccessStatusCode();

        using var firstClient = CreateAuthenticatedClient(organizer.Token);
        using var secondClient = CreateAuthenticatedClient(organizer.Token);
        using var quote = await SubmitQuoteAsync(firstClient, bookingId);
        quote.EnsureSuccessStatusCode();
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

    [Fact]
    public async Task Concurrent_reassignment_and_response_produce_one_consistent_winner()
    {
        await ResetAsync();
        var firstOrganizer = await CreateActorAsync("first-race-organizer@example.test", "Organizer");
        var secondOrganizer = await CreateActorAsync("second-race-organizer@example.test", "Organizer");
        var admin = await LoginAdminAsync();
        using var publicClient = CookieClient();
        SetClientAddress(publicClient, "203.0.113.25");
        using var submission = await SendWithCsrfAsync(publicClient, HttpMethod.Post, "/api/booking-requests", Payload());
        var bookingId = (await ReadJsonAsync(submission)).GetProperty("id").GetGuid();
        using var adminClient = CreateAuthenticatedClient(admin.Token);
        using var initialAssignment = await adminClient.PutAsJsonAsync(
            $"/api/booking-requests/{bookingId}/assign",
            new { organizerId = firstOrganizer.UserId });
        initialAssignment.EnsureSuccessStatusCode();
        using var organizerClient = CreateAuthenticatedClient(firstOrganizer.Token);
        using var quote = await SubmitQuoteAsync(organizerClient, bookingId);
        quote.EnsureSuccessStatusCode();
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var reassignTask = SendAfterGateAsync(gate.Task, () => adminClient.PutAsJsonAsync(
            $"/api/booking-requests/{bookingId}/assign",
            new { organizerId = secondOrganizer.UserId }));
        var respondTask = SendAfterGateAsync(gate.Task, () => organizerClient.PutAsJsonAsync(
            $"/api/booking-requests/{bookingId}/respond",
            new { accept = true, note = "Concurrent response." }));

        gate.SetResult();
        var responses = await Task.WhenAll(reassignTask, respondTask);
        try
        {
            Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
            Assert.Single(responses, response =>
                response.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.Forbidden);
            Assert.InRange(await Fixture.CountEventsAsync(), 0, 1);
        }
        finally
        {
            foreach (var response in responses) response.Dispose();
        }
    }

    [Fact]
    public async Task Closed_request_personal_data_is_anonymized_after_retention_window()
    {
        await ResetAsync();
        var id = await Fixture.CreateClosedBookingRequestAsync(DateTimeOffset.UtcNow.AddDays(-91));

        await Fixture.ApplyBookingRequestRetentionAsync();

        var data = await Fixture.GetBookingPersonalDataAsync(id);
        Assert.Equal("Removed", data.ContactName);
        Assert.EndsWith("@invalid.local", data.Email);
        Assert.Equal("Personal data removed under the retention policy.", data.Description);
        Assert.NotNull(data.AnonymizedAt);
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

    private static Task<HttpResponseMessage> SubmitQuoteAsync(HttpClient client, Guid bookingId) =>
        client.PostAsJsonAsync($"/api/booking-requests/{bookingId}/quote", new
        {
            proposedFeeMinor = 250000,
            proposedTimeline = "Four weeks",
            message = "Includes planning and delivery."
        });

    private static void SetClientAddress(HttpClient client, string address)
    {
        client.DefaultRequestHeaders.Add("X-Real-IP", address);
        client.DefaultRequestHeaders.Add("X-Forwarded-Proto", "https");
    }
}

public sealed class BookingRequestRateLimitTests(ApiIntegrationFixture fixture)
    : IntegrationTestBase(fixture), IClassFixture<ApiIntegrationFixture>
{
    [Fact]
    public async Task Public_submission_is_limited_to_five_requests_per_IP_per_hour()
    {
        await ResetAsync();
        using var client = CookieClient();
        client.DefaultRequestHeaders.Add("X-Real-IP", "203.0.113.26");
        client.DefaultRequestHeaders.Add("X-Forwarded-Proto", "https");
        for (var index = 0; index < 5; index++)
        {
            using var allowed = await SendWithCsrfAsync(client, HttpMethod.Post, "/api/booking-requests", Payload(index));
            Assert.Equal(HttpStatusCode.Accepted, allowed.StatusCode);
        }

        using var blocked = await SendWithCsrfAsync(client, HttpMethod.Post, "/api/booking-requests", Payload(6));
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
