using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using EventManagement.Api.Infrastructure;

namespace EventManagement.Api.IntegrationTests;

public sealed class AuthControllerTests(ApiIntegrationFixture fixture)
    : IntegrationTestBase(fixture), IClassFixture<ApiIntegrationFixture>
{
    [Fact]
    public async Task Login_returns_cryptographically_valid_JWT_with_current_role_claim()
    {
        await ResetAsync();
        var session = await LoginAdminAsync();

        var principal = ValidateToken(session.Token);

        Assert.Equal(session.UserId.ToString(), principal.FindFirstValue(JwtClaimNames.UserId));
        Assert.Equal("Admin", principal.FindFirstValue(JwtClaimNames.Role));
        Assert.True(principal.IsInRole("Admin"));
    }

    [Theory]
    [InlineData("invalid-token")]
    [InlineData("")]
    public async Task Invalid_or_missing_token_is_rejected_with_401(string token)
    {
        await ResetAsync();
        using var client = string.IsNullOrEmpty(token)
            ? Fixture.CreateClient()
            : CreateAuthenticatedClient(token);

        using var response = await client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Expired_token_is_rejected_with_401()
    {
        await ResetAsync();
        var student = await RegisterStudentAsync("expired@example.test");
        using var client = CreateAuthenticatedClient(CreateExpiredToken(student.Token));

        using var response = await client.GetAsync(
            $"/api/students/{student.UserId}/registrations");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("Student")]
    [InlineData("Organizer")]
    public async Task Authenticated_non_admin_is_forbidden_from_admin_endpoint(string role)
    {
        await ResetAsync();
        var actor = await CreateActorAsync($"wrong-role-{role}@example.test", role);
        using var client = CreateAuthenticatedClient(actor.Token);

        using var response = await client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Deactivated_users_existing_token_is_rejected_with_401()
    {
        await ResetAsync();
        var admin = await LoginAdminAsync();
        var student = await RegisterStudentAsync("deactivated@example.test");
        using (var adminClient = CreateAuthenticatedClient(admin.Token))
        using (var deactivation = await adminClient.PutAsync(
                   $"/api/users/{student.UserId}/deactivate", null))
        {
            Assert.Equal(HttpStatusCode.NoContent, deactivation.StatusCode);
        }
        using var studentClient = CreateAuthenticatedClient(student.Token);

        using var response = await studentClient.GetAsync(
            $"/api/students/{student.UserId}/registrations");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Role_change_requires_a_new_token_before_new_permissions_apply()
    {
        await ResetAsync();
        var admin = await LoginAdminAsync();
        var student = await RegisterStudentAsync("stale-role@example.test");
        await SetRoleAsync(admin.Token, student.UserId, "Organizer");
        using var staleClient = CreateAuthenticatedClient(student.Token);

        using var staleResponse = await staleClient.PostAsJsonAsync(
            "/api/events",
            EventPayload("Stale token event", 10));
        var refreshed = await LoginAsync("stale-role@example.test");
        using var refreshedClient = CreateAuthenticatedClient(refreshed.Token);
        using var refreshedResponse = await refreshedClient.PostAsJsonAsync(
            "/api/events",
            EventPayload("Fresh token event", 10));

        Assert.Equal(HttpStatusCode.Unauthorized, staleResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, refreshedResponse.StatusCode);
    }
}
