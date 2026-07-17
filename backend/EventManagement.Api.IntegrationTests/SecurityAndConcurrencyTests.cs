using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json;
using EventManagement.Api.Infrastructure;
using Microsoft.IdentityModel.Tokens;

namespace EventManagement.Api.IntegrationTests;

public sealed class SecurityAndConcurrencyTests(ApiIntegrationFixture fixture)
    : IClassFixture<ApiIntegrationFixture>
{
    private const string TestPassword = "Student-Integration-Password-123!";

    [Fact]
    public async Task Concurrent_registration_at_capacity_allows_exactly_one_student()
    {
        await fixture.ResetAsync();
        var admin = await LoginAsync(
            ApiIntegrationFixture.AdminEmail,
            ApiIntegrationFixture.AdminPassword);
        var firstStudent = await RegisterStudentAsync("capacity-one@example.test");
        var secondStudent = await RegisterStudentAsync("capacity-two@example.test");
        var eventId = await CreateEventAsync(admin.Token, "One remaining place", capacity: 1);

        using var firstClient = CreateAuthenticatedClient(firstStudent.Token);
        using var secondClient = CreateAuthenticatedClient(secondStudent.Token);
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstRequest = SendAfterGateAsync(
            gate.Task,
            () => firstClient.PostAsync($"/api/events/{eventId}/register", null));
        var secondRequest = SendAfterGateAsync(
            gate.Task,
            () => secondClient.PostAsync($"/api/events/{eventId}/register", null));

        gate.SetResult();
        var responses = await Task.WhenAll(firstRequest, secondRequest);
        try
        {
            Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Created);
            Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Conflict);
            Assert.Equal(1, await fixture.CountRegistrationsAsync(eventId));
        }
        finally
        {
            foreach (var response in responses) response.Dispose();
        }
    }

    [Fact]
    public async Task Organizer_cannot_edit_another_organizers_event()
    {
        await fixture.ResetAsync();
        var admin = await LoginAsync(
            ApiIntegrationFixture.AdminEmail,
            ApiIntegrationFixture.AdminPassword);
        var firstCandidate = await RegisterStudentAsync("organizer-one@example.test");
        var secondCandidate = await RegisterStudentAsync("organizer-two@example.test");
        await PromoteToOrganizerAsync(admin.Token, firstCandidate.UserId);
        await PromoteToOrganizerAsync(admin.Token, secondCandidate.UserId);
        var firstOrganizer = await LoginAsync(
            "organizer-one@example.test",
            TestPassword);
        var secondOrganizer = await LoginAsync(
            "organizer-two@example.test",
            TestPassword);
        var eventId = await CreateEventAsync(firstOrganizer.Token, "Owned by organizer one", 20);

        using var secondClient = CreateAuthenticatedClient(secondOrganizer.Token);
        using var response = await secondClient.PutAsJsonAsync(
            $"/api/events/{eventId}",
            EventPayload("Crafted cross-organizer edit", 20));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        using var publicClient = fixture.CreateClient();
        using var detailResponse = await publicClient.GetAsync($"/api/events/{eventId}");
        detailResponse.EnsureSuccessStatusCode();
        var detail = await ReadJsonAsync(detailResponse);
        Assert.Equal("Owned by organizer one", detail.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Demoted_organizer_with_owned_events_remains_in_organizer_report()
    {
        await fixture.ResetAsync();
        var admin = await LoginAsync(
            ApiIntegrationFixture.AdminEmail,
            ApiIntegrationFixture.AdminPassword);
        var formerOrganizer = await RegisterStudentAsync("former-organizer@example.test");
        var attendee = await RegisterStudentAsync("former-organizer-attendee@example.test");
        await PromoteToOrganizerAsync(admin.Token, formerOrganizer.UserId);
        var organizer = await LoginAsync("former-organizer@example.test", TestPassword);
        var eventId = await CreateEventAsync(
            organizer.Token,
            "Historical organizer report event",
            20);
        await RegisterForEventAsync(attendee.Token, eventId);
        await DemoteToStudentAsync(admin.Token, formerOrganizer.UserId);

        using var adminClient = CreateAuthenticatedClient(admin.Token);
        using var response = await adminClient.GetAsync("/api/reports/organizers");

        response.EnsureSuccessStatusCode();
        var report = await ReadJsonAsync(response);
        var organizerReport = report.EnumerateArray().Single(item =>
            item.GetProperty("organizerId").GetGuid() == formerOrganizer.UserId);
        Assert.Equal(1, organizerReport.GetProperty("eventCount").GetInt32());
        Assert.Equal(1, organizerReport.GetProperty("registrationCount").GetInt32());
    }

    [Fact]
    public async Task Organizer_cannot_delete_another_organizers_event()
    {
        await fixture.ResetAsync();
        var admin = await LoginAsync(
            ApiIntegrationFixture.AdminEmail,
            ApiIntegrationFixture.AdminPassword);
        var firstCandidate = await RegisterStudentAsync("delete-organizer-one@example.test");
        var secondCandidate = await RegisterStudentAsync("delete-organizer-two@example.test");
        await PromoteToOrganizerAsync(admin.Token, firstCandidate.UserId);
        await PromoteToOrganizerAsync(admin.Token, secondCandidate.UserId);
        var firstOrganizer = await LoginAsync(
            "delete-organizer-one@example.test",
            TestPassword);
        var secondOrganizer = await LoginAsync(
            "delete-organizer-two@example.test",
            TestPassword);
        var eventId = await CreateEventAsync(firstOrganizer.Token, "Delete ownership event", 20);

        using var secondClient = CreateAuthenticatedClient(secondOrganizer.Token);
        using var forbiddenResponse = await secondClient.DeleteAsync($"/api/events/{eventId}");

        Assert.Equal(HttpStatusCode.Forbidden, forbiddenResponse.StatusCode);
        using var publicClient = fixture.CreateClient();
        using var existingEventResponse = await publicClient.GetAsync($"/api/events/{eventId}");
        existingEventResponse.EnsureSuccessStatusCode();

        using var adminClient = CreateAuthenticatedClient(admin.Token);
        using var adminResponse = await adminClient.DeleteAsync($"/api/events/{eventId}");
        Assert.Equal(HttpStatusCode.NoContent, adminResponse.StatusCode);
        using var deletedEventResponse = await publicClient.GetAsync($"/api/events/{eventId}");
        Assert.Equal(HttpStatusCode.NotFound, deletedEventResponse.StatusCode);
    }

    [Fact]
    public async Task Organizer_cannot_view_another_organizers_registrants()
    {
        await fixture.ResetAsync();
        var admin = await LoginAsync(
            ApiIntegrationFixture.AdminEmail,
            ApiIntegrationFixture.AdminPassword);
        var firstCandidate = await RegisterStudentAsync("registrants-organizer-one@example.test");
        var secondCandidate = await RegisterStudentAsync("registrants-organizer-two@example.test");
        var attendee = await RegisterStudentAsync("registrants-attendee@example.test");
        await PromoteToOrganizerAsync(admin.Token, firstCandidate.UserId);
        await PromoteToOrganizerAsync(admin.Token, secondCandidate.UserId);
        var firstOrganizer = await LoginAsync(
            "registrants-organizer-one@example.test",
            TestPassword);
        var secondOrganizer = await LoginAsync(
            "registrants-organizer-two@example.test",
            TestPassword);
        var eventId = await CreateEventAsync(firstOrganizer.Token, "Registrant ownership event", 20);
        var registrationId = await RegisterForEventAsync(attendee.Token, eventId);

        using var secondClient = CreateAuthenticatedClient(secondOrganizer.Token);
        using var forbiddenResponse = await secondClient.GetAsync(
            $"/api/events/{eventId}/registrants");

        Assert.Equal(HttpStatusCode.Forbidden, forbiddenResponse.StatusCode);
        using var adminClient = CreateAuthenticatedClient(admin.Token);
        using var adminResponse = await adminClient.GetAsync($"/api/events/{eventId}/registrants");
        adminResponse.EnsureSuccessStatusCode();
        var registrants = await ReadJsonAsync(adminResponse);
        var registrant = registrants.EnumerateArray().Single();
        Assert.Equal(registrationId, registrant.GetProperty("registrationId").GetGuid());
    }

    [Fact]
    public async Task Organizer_cannot_update_attendance_for_another_organizers_event()
    {
        await fixture.ResetAsync();
        var admin = await LoginAsync(
            ApiIntegrationFixture.AdminEmail,
            ApiIntegrationFixture.AdminPassword);
        var firstCandidate = await RegisterStudentAsync("attendance-organizer-one@example.test");
        var secondCandidate = await RegisterStudentAsync("attendance-organizer-two@example.test");
        var attendee = await RegisterStudentAsync("attendance-attendee@example.test");
        await PromoteToOrganizerAsync(admin.Token, firstCandidate.UserId);
        await PromoteToOrganizerAsync(admin.Token, secondCandidate.UserId);
        var firstOrganizer = await LoginAsync(
            "attendance-organizer-one@example.test",
            TestPassword);
        var secondOrganizer = await LoginAsync(
            "attendance-organizer-two@example.test",
            TestPassword);
        var eventId = await CreateEventAsync(firstOrganizer.Token, "Attendance ownership event", 20);
        var registrationId = await RegisterForEventAsync(attendee.Token, eventId);
        var payload = new
        {
            registrations = new[]
            {
                new { registrationId, attended = true }
            }
        };

        using var secondClient = CreateAuthenticatedClient(secondOrganizer.Token);
        using var forbiddenResponse = await secondClient.PutAsJsonAsync(
            $"/api/events/{eventId}/attendance",
            payload);

        Assert.Equal(HttpStatusCode.Forbidden, forbiddenResponse.StatusCode);
        using var adminClient = CreateAuthenticatedClient(admin.Token);
        using var unchangedResponse = await adminClient.GetAsync(
            $"/api/events/{eventId}/registrants");
        unchangedResponse.EnsureSuccessStatusCode();
        var unchangedRegistrant = (await ReadJsonAsync(unchangedResponse))
            .EnumerateArray()
            .Single();
        Assert.False(unchangedRegistrant.GetProperty("attended").GetBoolean());

        using var adminResponse = await adminClient.PutAsJsonAsync(
            $"/api/events/{eventId}/attendance",
            payload);
        Assert.Equal(HttpStatusCode.NoContent, adminResponse.StatusCode);
        using var updatedResponse = await adminClient.GetAsync(
            $"/api/events/{eventId}/registrants");
        updatedResponse.EnsureSuccessStatusCode();
        var updatedRegistrant = (await ReadJsonAsync(updatedResponse))
            .EnumerateArray()
            .Single();
        Assert.True(updatedRegistrant.GetProperty("attended").GetBoolean());
    }

    [Fact]
    public async Task Student_direct_call_to_admin_users_endpoint_is_forbidden()
    {
        await fixture.ResetAsync();
        var student = await RegisterStudentAsync("not-an-admin@example.test");
        using var client = CreateAuthenticatedClient(student.Token);

        using var response = await client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Expired_JWT_is_rejected_by_protected_endpoint()
    {
        await fixture.ResetAsync();
        var student = await RegisterStudentAsync("expired-token@example.test");
        var expiredToken = CreateExpiredToken(student.Token);
        using var client = CreateAuthenticatedClient(expiredToken);

        using var response = await client.GetAsync(
            $"/api/students/{student.UserId}/registrations");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Deactivated_user_JWT_is_rejected_on_next_authenticated_request()
    {
        await fixture.ResetAsync();
        var admin = await LoginAsync(
            ApiIntegrationFixture.AdminEmail,
            ApiIntegrationFixture.AdminPassword);
        var student = await RegisterStudentAsync("deactivated-token@example.test");
        await DeactivateUserAsync(admin.Token, student.UserId);
        using var studentClient = CreateAuthenticatedClient(student.Token);

        using var response = await studentClient.GetAsync(
            $"/api/students/{student.UserId}/registrations");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await ReadJsonAsync(response);
        Assert.Equal("This account is inactive.", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task Stale_student_role_JWT_is_rejected_until_user_logs_in_again()
    {
        await fixture.ResetAsync();
        var admin = await LoginAsync(
            ApiIntegrationFixture.AdminEmail,
            ApiIntegrationFixture.AdminPassword);
        var student = await RegisterStudentAsync("stale-role@example.test");
        await PromoteToOrganizerAsync(admin.Token, student.UserId);
        var staleToken = new JwtSecurityTokenHandler().ReadJwtToken(student.Token);
        Assert.Equal(
            "Student",
            staleToken.Claims.Single(claim => claim.Type == JwtClaimNames.Role).Value);
        using var staleClient = CreateAuthenticatedClient(student.Token);

        using var staleResponse = await staleClient.PostAsJsonAsync(
            "/api/events",
            EventPayload("Rejected stale role event", 20));

        Assert.Equal(HttpStatusCode.Unauthorized, staleResponse.StatusCode);
        var staleBody = await ReadJsonAsync(staleResponse);
        Assert.Equal(
            "Your role changed. Sign in again to refresh your access.",
            staleBody.GetProperty("error").GetString());

        var refreshedOrganizer = await LoginAsync("stale-role@example.test", TestPassword);
        using var refreshedClient = CreateAuthenticatedClient(refreshedOrganizer.Token);
        using var refreshedResponse = await refreshedClient.PostAsJsonAsync(
            "/api/events",
            EventPayload("Accepted fresh role event", 20));
        Assert.Equal(HttpStatusCode.Created, refreshedResponse.StatusCode);
    }

    [Fact]
    public async Task Concurrent_duplicate_organizer_applications_create_one_pending_record()
    {
        await fixture.ResetAsync();
        var student = await RegisterStudentAsync("application-race@example.test");
        using var firstClient = CreateAuthenticatedClient(student.Token);
        using var secondClient = CreateAuthenticatedClient(student.Token);
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var payload = new
        {
            reason = "I want to organize practical events for the campus community."
        };
        var firstRequest = SendAfterGateAsync(
            gate.Task,
            () => firstClient.PostAsJsonAsync("/api/organizer-applications", payload));
        var secondRequest = SendAfterGateAsync(
            gate.Task,
            () => secondClient.PostAsJsonAsync("/api/organizer-applications", payload));

        gate.SetResult();
        var responses = await Task.WhenAll(firstRequest, secondRequest);
        try
        {
            Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Created);
            Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Conflict);
            Assert.Equal(1, await fixture.CountPendingApplicationsAsync(student.UserId));
        }
        finally
        {
            foreach (var response in responses) response.Dispose();
        }
    }

    private async Task<TestSession> RegisterStudentAsync(string email)
    {
        using var client = fixture.CreateClient();
        using var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                name = email.Split('@')[0],
                email,
                password = TestPassword
            });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return ReadSession(await ReadJsonAsync(response));
    }

    private async Task<TestSession> LoginAsync(string email, string password)
    {
        using var client = fixture.CreateClient();
        using var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { email, password });
        response.EnsureSuccessStatusCode();
        return ReadSession(await ReadJsonAsync(response));
    }

    private async Task PromoteToOrganizerAsync(string adminToken, Guid userId)
    {
        await UpdateUserRoleAsync(adminToken, userId, "Organizer");
    }

    private async Task DemoteToStudentAsync(string adminToken, Guid userId)
    {
        await UpdateUserRoleAsync(adminToken, userId, "Student");
    }

    private async Task UpdateUserRoleAsync(string adminToken, Guid userId, string role)
    {
        using var client = CreateAuthenticatedClient(adminToken);
        using var response = await client.PutAsJsonAsync(
            $"/api/users/{userId}/role",
            new { role });
        response.EnsureSuccessStatusCode();
    }

    private async Task DeactivateUserAsync(string adminToken, Guid userId)
    {
        using var client = CreateAuthenticatedClient(adminToken);
        using var response = await client.PutAsync($"/api/users/{userId}/deactivate", null);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    private async Task<Guid> CreateEventAsync(string token, string title, int capacity)
    {
        using var client = CreateAuthenticatedClient(token);
        using var response = await client.PostAsJsonAsync(
            "/api/events",
            EventPayload(title, capacity));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await ReadJsonAsync(response);
        return body.GetProperty("id").GetGuid();
    }

    private async Task<Guid> RegisterForEventAsync(string token, Guid eventId)
    {
        using var client = CreateAuthenticatedClient(token);
        using var response = await client.PostAsync($"/api/events/{eventId}/register", null);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await ReadJsonAsync(response);
        return body.GetProperty("registrationId").GetGuid();
    }

    private HttpClient CreateAuthenticatedClient(string token)
    {
        var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static object EventPayload(string title, int capacity) => new
    {
        title,
        description = "A sufficiently detailed integration-test event description.",
        date = DateTimeOffset.UtcNow.AddDays(7),
        location = "Integration Test Hall",
        capacity,
        category = "Technology"
    };

    private static async Task<HttpResponseMessage> SendAfterGateAsync(
        Task gate,
        Func<Task<HttpResponseMessage>> send)
    {
        await gate;
        return await send();
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        return document.RootElement.Clone();
    }

    private static string CreateExpiredToken(string validToken)
    {
        var handler = new JwtSecurityTokenHandler();
        var source = handler.ReadJwtToken(validToken);
        var claims = source.Claims.Where(claim =>
            claim.Type != JwtRegisteredClaimNames.Exp &&
            claim.Type != JwtRegisteredClaimNames.Nbf &&
            claim.Type != JwtRegisteredClaimNames.Iat);
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ApiWebApplicationFactory.JwtKey)),
            SecurityAlgorithms.HmacSha256);
        var expiredToken = new JwtSecurityToken(
            source.Issuer,
            source.Audiences.Single(),
            claims,
            notBefore: DateTime.UtcNow.AddMinutes(-10),
            expires: DateTime.UtcNow.AddMinutes(-5),
            signingCredentials: credentials);
        return handler.WriteToken(expiredToken);
    }

    private static TestSession ReadSession(JsonElement body) => new(
        body.GetProperty("token").GetString()
            ?? throw new InvalidOperationException("The API returned no token."),
        body.GetProperty("user").GetProperty("id").GetGuid());

    private sealed record TestSession(string Token, Guid UserId);
}
