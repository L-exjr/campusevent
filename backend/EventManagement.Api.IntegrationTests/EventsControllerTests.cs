using System.Net;
using System.Net.Http.Json;

namespace EventManagement.Api.IntegrationTests;

public sealed class EventsControllerTests(ApiIntegrationFixture fixture)
    : IntegrationTestBase(fixture), IClassFixture<ApiIntegrationFixture>
{
    [Fact]
    public async Task Unpublished_management_detail_is_limited_to_owner_or_admin()
    {
        await ResetAsync();
        var owner = await CreateActorAsync("draft-owner@example.test", "Organizer");
        var other = await CreateActorAsync("draft-other@example.test", "Organizer");
        var eventId = await CreateEventAsync(owner.Token, "Management-only draft", 10);
        using var ownerClient = CreateAuthenticatedClient(owner.Token);
        using var unpublish = await ownerClient.PutAsJsonAsync(
            $"/api/events/{eventId}",
            new
            {
                title = "Management-only draft",
                description = "An unpublished event used to verify management detail authorization.",
                date = DateTimeOffset.UtcNow.AddDays(7),
                location = "Draft Hall",
                capacity = 10,
                category = "Technology",
                isPublished = false
            });
        unpublish.EnsureSuccessStatusCode();

        using var publicClient = Fixture.CreateClient();
        using var publicResponse = await publicClient.GetAsync($"/api/events/{eventId}");
        using var ownerResponse = await ownerClient.GetAsync($"/api/events/{eventId}/management");
        using var otherClient = CreateAuthenticatedClient(other.Token);
        using var otherResponse = await otherClient.GetAsync($"/api/events/{eventId}/management");
        var admin = await LoginAdminAsync();
        using var adminClient = CreateAuthenticatedClient(admin.Token);
        using var adminResponse = await adminClient.GetAsync($"/api/events/{eventId}/management");

        Assert.Equal(HttpStatusCode.NotFound, publicResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, ownerResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, otherResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, adminResponse.StatusCode);
        Assert.False((await ReadJsonAsync(ownerResponse)).GetProperty("isPublished").GetBoolean());
    }

    [Fact]
    public async Task Non_admin_cannot_list_unpublished_events()
    {
        await ResetAsync();
        var student = await RegisterStudentAsync("draft-list-student@example.test");
        using var anonymousClient = Fixture.CreateClient();
        using var anonymous = await anonymousClient.GetAsync("/api/events/all");
        using var studentClient = CreateAuthenticatedClient(student.Token);
        using var studentResponse = await studentClient.GetAsync("/api/events/all");

        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, studentResponse.StatusCode);
    }

    [Fact]
    public async Task Past_dated_draft_cannot_be_published()
    {
        await ResetAsync();
        var owner = await CreateActorAsync("past-draft-owner@example.test", "Organizer");
        var eventId = await CreateEventAsync(owner.Token, "Past draft", 10);
        using var client = CreateAuthenticatedClient(owner.Token);
        using var unpublish = await client.PutAsJsonAsync(
            $"/api/events/{eventId}",
            EventUpdatePayload("Past draft", DateTimeOffset.UtcNow.AddDays(7), false));
        unpublish.EnsureSuccessStatusCode();
        var pastDate = DateTimeOffset.UtcNow.AddDays(-1);
        await Fixture.SetEventDateAsync(eventId, pastDate);

        using var publish = await client.PutAsJsonAsync(
            $"/api/events/{eventId}",
            EventUpdatePayload("Past draft", pastDate, true));

        Assert.Equal(HttpStatusCode.BadRequest, publish.StatusCode);
        Assert.False((await Fixture.GetEventStateAsync(eventId)).IsPublished);
    }

    [Fact]
    public async Task Upcoming_mine_filter_excludes_past_events()
    {
        await ResetAsync();
        var organizer = await CreateActorAsync("upcoming-owner@example.test", "Organizer");
        var pastEvent = await CreateEventAsync(organizer.Token, "Past organizer event", 10);
        var futureEvent = await CreateEventAsync(organizer.Token, "Future organizer event", 10);
        await Fixture.SetEventDateAsync(pastEvent, DateTimeOffset.UtcNow.AddDays(-1));
        using var client = CreateAuthenticatedClient(organizer.Token);

        using var response = await client.GetAsync("/api/events/mine?upcoming=true");

        response.EnsureSuccessStatusCode();
        var items = (await ReadJsonAsync(response)).GetProperty("items").EnumerateArray().ToList();
        Assert.Single(items);
        Assert.Equal(futureEvent, items[0].GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task Event_cover_image_URL_round_trips_through_create_and_read()
    {
        await ResetAsync();
        var organizer = await CreateActorAsync("image-organizer@example.test", "Organizer");
        using var client = CreateAuthenticatedClient(organizer.Token);
        using var upload = await client.PostAsync("/api/uploads/event-image", CreatePngUpload());
        upload.EnsureSuccessStatusCode();
        var imageUrl = (await ReadJsonAsync(upload)).GetProperty("url").GetString()!;
        var payload = new
        {
            title = "Event with cover",
            description = "An event with a persisted public cover image URL.",
            date = DateTimeOffset.UtcNow.AddDays(7),
            location = "Image Hall",
            capacity = 20,
            category = "Technology",
            imageUrl
        };

        using var created = await client.PostAsJsonAsync("/api/events", payload);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var eventId = (await ReadJsonAsync(created)).GetProperty("id").GetGuid();
        using var publicClient = Fixture.CreateClient();
        using var fetched = await publicClient.GetAsync($"/api/events/{eventId}");

        fetched.EnsureSuccessStatusCode();
        Assert.Equal(imageUrl, (await ReadJsonAsync(fetched)).GetProperty("imageUrl").GetString());
    }

    [Theory]
    [InlineData("update")]
    [InlineData("delete")]
    public async Task Organizer_cannot_modify_another_organizers_event(string operation)
    {
        await ResetAsync();
        var owner = await CreateActorAsync("owner@example.test", "Organizer");
        var attacker = await CreateActorAsync("attacker@example.test", "Organizer");
        var eventId = await CreateEventAsync(owner.Token, "Owner's unchanged event", 10);
        using var attackerClient = CreateAuthenticatedClient(attacker.Token);

        using var response = operation == "update"
            ? await attackerClient.PutAsJsonAsync(
                $"/api/events/{eventId}",
                EventPayload("Crafted cross-owner update", 10))
            : await attackerClient.DeleteAsync($"/api/events/{eventId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        using var publicClient = Fixture.CreateClient();
        using var persisted = await publicClient.GetAsync($"/api/events/{eventId}");
        persisted.EnsureSuccessStatusCode();
        var body = await ReadJsonAsync(persisted);
        Assert.Equal("Owner's unchanged event", body.GetProperty("title").GetString());
    }

    [Theory]
    [InlineData("update")]
    [InlineData("delete")]
    public async Task Admin_can_modify_any_organizers_event(string operation)
    {
        await ResetAsync();
        var owner = await CreateActorAsync($"admin-target-{operation}@example.test", "Organizer");
        var eventId = await CreateEventAsync(owner.Token, "Organizer-owned event", 10);
        var admin = await LoginAdminAsync();
        using var adminClient = CreateAuthenticatedClient(admin.Token);

        using var response = operation == "update"
            ? await adminClient.PutAsJsonAsync(
                $"/api/events/{eventId}",
                EventPayload("Admin updated event", 10))
            : await adminClient.DeleteAsync($"/api/events/{eventId}");

        Assert.Equal(
            operation == "update" ? HttpStatusCode.OK : HttpStatusCode.NoContent,
            response.StatusCode);
        using var publicClient = Fixture.CreateClient();
        using var persisted = await publicClient.GetAsync($"/api/events/{eventId}");
        if (operation == "delete")
        {
            Assert.Equal(HttpStatusCode.NotFound, persisted.StatusCode);
        }
        else
        {
            persisted.EnsureSuccessStatusCode();
            Assert.Equal(
                "Admin updated event",
                (await ReadJsonAsync(persisted)).GetProperty("title").GetString());
        }
    }

    [Fact]
    public async Task Student_cannot_register_twice_for_same_event()
    {
        await ResetAsync();
        var admin = await LoginAdminAsync();
        var student = await RegisterStudentAsync("duplicate-registration@example.test");
        var eventId = await CreateEventAsync(admin.Token, "Duplicate registration event", 5);
        using var client = CreateAuthenticatedClient(student.Token);

        using var first = await client.PostAsync($"/api/events/{eventId}/register", null);
        using var second = await client.PostAsync($"/api/events/{eventId}/register", null);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal(1, await Fixture.CountRegistrationsAsync(eventId));
    }

    [Fact]
    public async Task Registration_is_rejected_after_capacity_is_reached()
    {
        await ResetAsync();
        var admin = await LoginAdminAsync();
        var admitted = await RegisterStudentAsync("capacity-admitted@example.test");
        var rejected = await RegisterStudentAsync("capacity-rejected@example.test");
        var eventId = await CreateEventAsync(admin.Token, "Full event", 1);
        await RegisterForEventAsync(admitted.Token, eventId);
        using var client = CreateAuthenticatedClient(rejected.Token);

        using var response = await client.PostAsync($"/api/events/{eventId}/register", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(1, await Fixture.CountRegistrationsAsync(eventId));
    }

    [Fact]
    public async Task Concurrent_registration_at_capacity_never_overbooks()
    {
        await ResetAsync();
        var admin = await LoginAdminAsync();
        var firstStudent = await RegisterStudentAsync("capacity-one@example.test");
        var secondStudent = await RegisterStudentAsync("capacity-two@example.test");
        var eventId = await CreateEventAsync(admin.Token, "One remaining place", 1);
        using var firstClient = CreateAuthenticatedClient(firstStudent.Token);
        using var secondClient = CreateAuthenticatedClient(secondStudent.Token);
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstRequest = SendAfterGateAsync(gate.Task, () =>
            firstClient.PostAsync($"/api/events/{eventId}/register", null));
        var secondRequest = SendAfterGateAsync(gate.Task, () =>
            secondClient.PostAsync($"/api/events/{eventId}/register", null));

        gate.SetResult();
        var responses = await Task.WhenAll(firstRequest, secondRequest);
        try
        {
            Assert.Single(responses, item => item.StatusCode == HttpStatusCode.Created);
            Assert.Single(responses, item => item.StatusCode == HttpStatusCode.Conflict);
            Assert.Equal(1, await Fixture.CountRegistrationsAsync(eventId));
        }
        finally
        {
            foreach (var response in responses) response.Dispose();
        }
    }

    [Theory]
    [InlineData("Owner", HttpStatusCode.NoContent)]
    [InlineData("OtherOrganizer", HttpStatusCode.Forbidden)]
    [InlineData("Admin", HttpStatusCode.NoContent)]
    public async Task Attendance_permission_matrix(string actorKind, HttpStatusCode expected)
    {
        await ResetAsync();
        var owner = await CreateActorAsync($"attendance-owner-{actorKind}@example.test", "Organizer");
        var attendee = await RegisterStudentAsync($"attendance-student-{actorKind}@example.test");
        var eventId = await CreateEventAsync(owner.Token, "Attendance event", 10);
        var registrationId = await RegisterForEventAsync(attendee.Token, eventId);
        var actor = actorKind switch
        {
            "Owner" => owner,
            "OtherOrganizer" => await CreateActorAsync(
                $"attendance-other-{actorKind}@example.test", "Organizer"),
            _ => await LoginAdminAsync()
        };
        using var client = CreateAuthenticatedClient(actor.Token);

        using var response = await client.PutAsJsonAsync(
            $"/api/events/{eventId}/attendance",
            new { registrations = new[] { new { registrationId, attended = true } } });

        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public async Task Attendance_rejects_student_registration_that_does_not_belong_to_event()
    {
        await ResetAsync();
        var owner = await CreateActorAsync("attendance-owner@example.test", "Organizer");
        var attendee = await RegisterStudentAsync("attendance-unregistered@example.test");
        var targetEvent = await CreateEventAsync(owner.Token, "Target attendance event", 10);
        var otherEvent = await CreateEventAsync(owner.Token, "Other attendance event", 10);
        var otherRegistration = await RegisterForEventAsync(attendee.Token, otherEvent);
        using var client = CreateAuthenticatedClient(owner.Token);

        using var response = await client.PutAsJsonAsync(
            $"/api/events/{targetEvent}/attendance",
            new
            {
                registrations = new[]
                {
                    new { registrationId = otherRegistration, attended = true }
                }
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Organizer_cannot_view_another_organizers_registrants()
    {
        await ResetAsync();
        var owner = await CreateActorAsync("registrants-owner@example.test", "Organizer");
        var otherOrganizer = await CreateActorAsync("registrants-other@example.test", "Organizer");
        var attendee = await RegisterStudentAsync("registrants-attendee@example.test");
        var eventId = await CreateEventAsync(owner.Token, "Private registrant list", 10);
        await RegisterForEventAsync(attendee.Token, eventId);
        using var otherClient = CreateAuthenticatedClient(otherOrganizer.Token);

        using var forbidden = await otherClient.GetAsync($"/api/events/{eventId}/registrants");
        var admin = await LoginAdminAsync();
        using var adminClient = CreateAuthenticatedClient(admin.Token);
        using var allowed = await adminClient.GetAsync($"/api/events/{eventId}/registrants");

        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
    }

    private static object EventUpdatePayload(string title, DateTimeOffset date, bool isPublished) => new
    {
        title,
        description = "A sufficiently detailed integration-test event description.",
        date,
        location = "Integration Test Hall",
        capacity = 10,
        category = "Technology",
        isPublished
    };
}
