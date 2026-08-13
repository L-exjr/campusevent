using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using EventManagement.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EventManagement.Api.IntegrationTests;

public sealed class AuthControllerTests(ApiIntegrationFixture fixture)
    : IntegrationTestBase(fixture), IClassFixture<ApiIntegrationFixture>
{
    [Fact]
    public async Task Login_issues_secure_httponly_cross_site_cookie_without_returning_token()
    {
        await ResetAsync();
        using var client = CookieClient();
        await RegisterWithCsrfAsync(client, "cookie-login@example.test");
        var csrf = await GetCsrfAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login") {
            Content = JsonContent.Create(new { email = "cookie-login@example.test", password = TestPassword })
        };
        request.Headers.Add("X-CSRF-TOKEN", csrf);
        using var response = await client.SendAsync(request);
        var body = await ReadJsonAsync(response);

        response.EnsureSuccessStatusCode();
        Assert.False(body.TryGetProperty("token", out _));
        var cookie = response.Headers.GetValues("Set-Cookie").Single(value => value.StartsWith("campus_events_session="));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=none", cookie, StringComparison.OrdinalIgnoreCase);
        using var session = await client.GetAsync("/api/auth/session");
        Assert.Equal(HttpStatusCode.OK, session.StatusCode);
    }

    [Fact]
    public async Task State_changing_request_rejects_missing_csrf_token()
    {
        await ResetAsync();
        using var client = CookieClient();
        using var response = await client.PostAsJsonAsync("/api/auth/register", new {
            name = "Missing CSRF", email = "missing-csrf@example.test", password = TestPassword
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("The CSRF token is missing or invalid.", (await ReadJsonAsync(response)).GetProperty("error").GetString());
    }

    [Fact]
    public async Task State_changing_request_rejects_invalid_csrf_token()
    {
        using var client = CookieClient();
        await GetCsrfAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/register") {
            Content = JsonContent.Create(new { name = "Invalid CSRF", email = "invalid-csrf@example.test", password = TestPassword })
        };
        request.Headers.Add("X-CSRF-TOKEN", "invalid-token");
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Expired_session_cookie_is_rejected()
    {
        await ResetAsync();
        var valid = await LoginAdminAsync();
        var expired = CreateExpiredToken(valid.Token);
        using var client = CookieClient();
        client.DefaultRequestHeaders.Add("Cookie", $"campus_events_session={expired}");
        using var response = await client.GetAsync("/api/auth/session");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_expires_cookie_and_clears_authenticated_session()
    {
        await ResetAsync();
        using var client = CookieClient();
        await RegisterWithCsrfAsync(client, "cookie-logout@example.test");
        var csrf = await GetCsrfAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        request.Headers.Add("X-CSRF-TOKEN", csrf);
        using var logout = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Contains(logout.Headers.GetValues("Set-Cookie"), value =>
            value.StartsWith("campus_events_session=") && value.Contains("expires=", StringComparison.OrdinalIgnoreCase));
        using var session = await client.GetAsync("/api/auth/session");
        Assert.Equal(HttpStatusCode.Unauthorized, session.StatusCode);
    }

    [Fact]
    public async Task Cors_allows_configured_frontend_credentials_but_not_unknown_origins()
    {
        using var client = CookieClient();
        using var allowedRequest = new HttpRequestMessage(HttpMethod.Options, "/api/auth/login");
        allowedRequest.Headers.Add("Origin", "http://localhost:5173");
        allowedRequest.Headers.Add("Access-Control-Request-Method", "POST");
        using var allowed = await client.SendAsync(allowedRequest);
        Assert.Equal("http://localhost:5173", allowed.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.Equal("true", allowed.Headers.GetValues("Access-Control-Allow-Credentials").Single());

        using var rejectedRequest = new HttpRequestMessage(HttpMethod.Options, "/api/auth/login");
        rejectedRequest.Headers.Add("Origin", "https://untrusted.example");
        rejectedRequest.Headers.Add("Access-Control-Request-Method", "POST");
        using var rejected = await client.SendAsync(rejectedRequest);
        Assert.False(rejected.Headers.Contains("Access-Control-Allow-Origin"));
    }

    private static async Task<string> GetCsrfAsync(HttpClient client)
    {
        using var response = await client.GetAsync("/api/auth/csrf");
        response.EnsureSuccessStatusCode();
        return (await ReadJsonAsync(response)).GetProperty("token").GetString()!;
    }

    private static async Task RegisterWithCsrfAsync(HttpClient client, string email)
    {
        var csrf = await GetCsrfAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/register") {
            Content = JsonContent.Create(new { name = "Cookie User", email, password = TestPassword })
        };
        request.Headers.Add("X-CSRF-TOKEN", csrf);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Forgot_password_returns_same_message_for_known_and_unknown_email()
    {
        await ResetAsync();
        await RegisterStudentAsync("known-reset@example.test");
        using var client = CookieClient();
        using var known = await SendWithCsrfAsync(client, HttpMethod.Post, "/api/auth/forgot-password", new { email = "known-reset@example.test" });
        using var unknown = await SendWithCsrfAsync(client, HttpMethod.Post, "/api/auth/forgot-password", new { email = "unknown-reset@example.test" });

        known.EnsureSuccessStatusCode(); unknown.EnsureSuccessStatusCode();
        Assert.Equal(
            (await ReadJsonAsync(known)).GetProperty("message").GetString(),
            (await ReadJsonAsync(unknown)).GetProperty("message").GetString());
        Assert.Equal(1, await Fixture.CountEmailOutboxMessagesAsync("PasswordReset"));
    }

    [Fact]
    public async Task Reset_token_changes_password_and_is_single_use()
    {
        await ResetAsync();
        var student = await RegisterStudentAsync("reset-once@example.test");
        var token = await Fixture.CreateResetTokenAsync(student.UserId, DateTimeOffset.UtcNow.AddMinutes(30));
        using var client = CookieClient();
        using var reset = await SendWithCsrfAsync(client, HttpMethod.Post, "/api/auth/reset-password", new { token, newPassword = "New-Integration-Password-123!" });
        using var reuse = await SendWithCsrfAsync(client, HttpMethod.Post, "/api/auth/reset-password", new { token, newPassword = "Another-Integration-Password-123!" });
        using var login = await SendWithCsrfAsync(client, HttpMethod.Post, "/api/auth/login", new { email = "reset-once@example.test", password = "New-Integration-Password-123!" });

        Assert.Equal(HttpStatusCode.OK, reset.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, reuse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task Password_reset_revokes_existing_sessions()
    {
        await ResetAsync();
        var student = await RegisterStudentAsync("reset-revokes@example.test");
        var token = await Fixture.CreateResetTokenAsync(
            student.UserId,
            DateTimeOffset.UtcNow.AddMinutes(30));
        using var anonymousClient = CookieClient();
        using var reset = await SendWithCsrfAsync(anonymousClient, HttpMethod.Post,
            "/api/auth/reset-password", new { token, newPassword = "New-Integration-Password-123!" });
        reset.EnsureSuccessStatusCode();
        using var staleClient = CreateAuthenticatedClient(student.Token);

        using var staleResponse = await staleClient.GetAsync(
            $"/api/students/{student.UserId}/registrations");
        var refreshed = await LoginAsync(
            "reset-revokes@example.test",
            "New-Integration-Password-123!");
        using var refreshedClient = CreateAuthenticatedClient(refreshed.Token);
        using var refreshedResponse = await refreshedClient.GetAsync(
            $"/api/students/{student.UserId}/registrations");

        Assert.Equal(HttpStatusCode.Unauthorized, staleResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, refreshedResponse.StatusCode);
    }

    [Fact]
    public async Task Expired_reset_token_is_rejected()
    {
        await ResetAsync();
        var student = await RegisterStudentAsync("expired-reset@example.test");
        var token = await Fixture.CreateResetTokenAsync(student.UserId, DateTimeOffset.UtcNow.AddMinutes(-1));
        using var client = Fixture.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/auth/reset-password", new { token, newPassword = "New-Integration-Password-123!" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Google_login_links_existing_email_without_duplicate_and_uses_same_user_id()
    {
        await ResetAsync();
        var local = await RegisterStudentAsync("linked@example.test");
        using var client = CookieClient();
        using var response = await SendWithCsrfAsync(client, HttpMethod.Post, "/api/auth/google", new { idToken = "google-subject|LINKED@example.test|Linked User" });
        response.EnsureSuccessStatusCode();
        var body = await ReadJsonAsync(response);
        using var repeated = await SendWithCsrfAsync(client, HttpMethod.Post, "/api/auth/google", new { idToken = "google-subject|linked@example.test|Linked User" });
        using var passwordLogin = await SendWithCsrfAsync(client, HttpMethod.Post, "/api/auth/login", new { email = "linked@example.test", password = TestPassword });

        Assert.Equal(local.UserId, body.GetProperty("user").GetProperty("id").GetGuid());
        Assert.Equal(HttpStatusCode.OK, repeated.StatusCode);
        Assert.Equal(HttpStatusCode.OK, passwordLogin.StatusCode);
        Assert.Equal(1, await Fixture.CountUsersByEmailAsync("linked@example.test"));
    }

    [Fact]
    public async Task Google_login_creates_only_a_Student_for_a_new_email()
    {
        await ResetAsync();
        using var client = CookieClient();
        using var response = await SendWithCsrfAsync(client, HttpMethod.Post, "/api/auth/google", new { idToken = "new-google-subject|new-google@example.test|New Google User" });
        response.EnsureSuccessStatusCode();
        Assert.Equal("Student", (await ReadJsonAsync(response)).GetProperty("user").GetProperty("role").GetString());
    }
    [Fact]
    public async Task Login_returns_cryptographically_valid_JWT_with_current_role_claim()
    {
        await ResetAsync();
        var session = await LoginAdminAsync();

        var principal = ValidateToken(session.Token);

        Assert.Equal(session.UserId.ToString(), principal.FindFirstValue(JwtClaimNames.UserId));
        Assert.Equal("Admin", principal.FindFirstValue(JwtClaimNames.Role));
        Assert.Equal("1", principal.FindFirstValue(JwtClaimNames.SessionVersion));
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

public sealed class AuthRateLimitTests(ApiIntegrationFixture fixture)
    : IntegrationTestBase(fixture), IClassFixture<ApiIntegrationFixture>
{
    [Fact]
    public async Task Login_is_limited_by_forwarded_client_IP()
    {
        await ResetAsync();
        const string forwardedAddress = "203.0.113.10";
        await Fixture.SetAuthRateLimitCountAsync("Ip", "Login", forwardedAddress, 10000);
        using var client = CookieClient();
        client.DefaultRequestHeaders.Add("X-Real-IP", forwardedAddress);
        client.DefaultRequestHeaders.Add("X-Forwarded-Proto", "https");

        using var response = await SendWithCsrfAsync(client, HttpMethod.Post, "/api/auth/login",
            new { email = ApiIntegrationFixture.AdminEmail, password = ApiIntegrationFixture.AdminPassword });

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
    }

    [Fact]
    public async Task Login_is_limited_by_account_across_client_IPs()
    {
        await ResetAsync();
        await Fixture.SetAuthRateLimitCountAsync(
            "Account",
            "Login",
            ApiIntegrationFixture.AdminEmail,
            10000);
        using var client = CookieClient();
        client.DefaultRequestHeaders.Add("X-Real-IP", "203.0.113.11");
        client.DefaultRequestHeaders.Add("X-Forwarded-Proto", "https");

        using var response = await SendWithCsrfAsync(client, HttpMethod.Post, "/api/auth/login",
            new { email = ApiIntegrationFixture.AdminEmail, password = ApiIntegrationFixture.AdminPassword });

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
    }
}
