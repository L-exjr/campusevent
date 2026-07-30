using System.Net;
using System.Net.Http.Json;

namespace EventManagement.Api.IntegrationTests;

public sealed class EventsControllerTests(ApiIntegrationFixture fixture)
    : IntegrationTestBase(fixture), IClassFixture<ApiIntegrationFixture>
{
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
    public async Task Event_cover_image_URL_round_trips_through_create_and_read()
    {
        await ResetAsync();
        var organizer = await CreateActorAsync("image-organizer@example.test", "Organizer");
        using var client = CreateAuthenticatedClient(organizer.Token);
        const string imageUrl = "https://project.supabase.co/storage/v1/object/public/event-images/cover.webp";
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
}
