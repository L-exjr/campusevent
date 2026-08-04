using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using EventManagement.Api.Infrastructure;

namespace EventManagement.Api.IntegrationTests;

public sealed class OrganizerApplicationsControllerTests(ApiIntegrationFixture fixture)
    : IntegrationTestBase(fixture), IClassFixture<ApiIntegrationFixture>
{
    [Fact]
    public async Task Student_can_submit_once_but_second_pending_application_is_rejected()
    {
        await ResetAsync();
        var student = await RegisterStudentAsync("applicant@example.test");
        using var client = CreateAuthenticatedClient(student.Token);

        using var first = await client.PostAsJsonAsync(
            "/api/organizer-applications",
            ApplicationPayload());
        using var second = await client.PostAsJsonAsync(
            "/api/organizer-applications",
            ApplicationPayload());

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal(1, await Fixture.CountPendingApplicationsAsync(student.UserId));
    }

    [Theory]
    [InlineData("Student", "approve")]
    [InlineData("Organizer", "approve")]
    [InlineData("Student", "reject")]
    [InlineData("Organizer", "reject")]
    public async Task Non_admin_cannot_review_application(string role, string action)
    {
        await ResetAsync();
        var applicant = await RegisterStudentAsync($"review-{role}-{action}@example.test");
        var applicationId = await SubmitApplicationAsync(applicant.Token);
        var actor = await CreateActorAsync($"reviewer-{role}-{action}@example.test", role);
        using var client = CreateAuthenticatedClient(actor.Token);

        using var response = action == "approve"
            ? await client.PutAsync($"/api/organizer-applications/{applicationId}/approve", null)
            : await client.PutAsJsonAsync(
                $"/api/organizer-applications/{applicationId}/reject",
                new { reason = "Not enough detail." });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(1, await Fixture.CountPendingApplicationsAsync(applicant.UserId));
    }

    [Fact]
    public async Task Admin_approval_changes_role_and_new_login_issues_organizer_JWT()
    {
        await ResetAsync();
        var applicant = await RegisterStudentAsync("approved@example.test");
        var applicationId = await SubmitApplicationAsync(applicant.Token);
        var admin = await LoginAdminAsync();
        using var adminClient = CreateAuthenticatedClient(admin.Token);

        using var approval = await adminClient.PutAsync(
            $"/api/organizer-applications/{applicationId}/approve",
            null);
        var refreshed = await LoginAsync("approved@example.test");
        var principal = ValidateToken(refreshed.Token);

        Assert.Equal(HttpStatusCode.OK, approval.StatusCode);
        Assert.Equal(1, await Fixture.CountEmailOutboxMessagesAsync("OrganizerApplicationDecision"));
        Assert.Equal("Organizer", principal.FindFirstValue(JwtClaimNames.Role));
        using var organizerClient = CreateAuthenticatedClient(refreshed.Token);
        using var createEvent = await organizerClient.PostAsJsonAsync(
            "/api/events",
            EventPayload("Approved organizer event", 10));
        Assert.Equal(HttpStatusCode.Created, createEvent.StatusCode);
    }

    [Fact]
    public async Task Admin_can_reject_application_without_changing_student_role()
    {
        await ResetAsync();
        var applicant = await RegisterStudentAsync("rejected@example.test");
        var applicationId = await SubmitApplicationAsync(applicant.Token);
        var admin = await LoginAdminAsync();
        using var client = CreateAuthenticatedClient(admin.Token);

        using var response = await client.PutAsJsonAsync(
            $"/api/organizer-applications/{applicationId}/reject",
            new { reason = "More event planning experience is required." });
        var refreshed = await LoginAsync("rejected@example.test");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, await Fixture.CountEmailOutboxMessagesAsync("OrganizerApplicationDecision"));
        Assert.Equal("Student", ValidateToken(refreshed.Token).FindFirstValue(JwtClaimNames.Role));
    }

    [Fact]
    public async Task Concurrent_submissions_create_only_one_pending_application()
    {
        await ResetAsync();
        var student = await RegisterStudentAsync("application-race@example.test");
        using var firstClient = CreateAuthenticatedClient(student.Token);
        using var secondClient = CreateAuthenticatedClient(student.Token);
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstRequest = SendAfterGateAsync(gate.Task, () =>
            firstClient.PostAsJsonAsync("/api/organizer-applications", ApplicationPayload()));
        var secondRequest = SendAfterGateAsync(gate.Task, () =>
            secondClient.PostAsJsonAsync("/api/organizer-applications", ApplicationPayload()));

        gate.SetResult();
        var responses = await Task.WhenAll(firstRequest, secondRequest);
        try
        {
            Assert.Single(responses, item => item.StatusCode == HttpStatusCode.Created);
            Assert.Single(responses, item => item.StatusCode == HttpStatusCode.Conflict);
            Assert.Equal(1, await Fixture.CountPendingApplicationsAsync(student.UserId));
        }
        finally
        {
            foreach (var response in responses) response.Dispose();
        }
    }
}
