using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using EventManagement.Api.Infrastructure;
using Microsoft.IdentityModel.Tokens;

namespace EventManagement.Api.IntegrationTests;

public abstract class IntegrationTestBase(ApiIntegrationFixture fixture)
{
    protected const string TestPassword = "Student-Integration-Password-123!";
    protected ApiIntegrationFixture Fixture { get; } = fixture;

    protected Task ResetAsync() => Fixture.ResetAsync();

    protected async Task<TestSession> RegisterStudentAsync(string email)
    {
        using var client = Fixture.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            name = email.Split('@')[0],
            email,
            password = TestPassword
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return ReadSession(await ReadJsonAsync(response));
    }

    protected async Task<TestSession> LoginAsync(string email, string password = TestPassword)
    {
        using var client = Fixture.CreateClient();
        using var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { email, password });
        response.EnsureSuccessStatusCode();
        return ReadSession(await ReadJsonAsync(response));
    }

    protected Task<TestSession> LoginAdminAsync() => LoginAsync(
        ApiIntegrationFixture.AdminEmail,
        ApiIntegrationFixture.AdminPassword);

    protected async Task<TestSession> CreateActorAsync(string email, string role)
    {
        if (role == "Admin") return await LoginAdminAsync();
        var student = await RegisterStudentAsync(email);
        if (role == "Student") return student;

        var admin = await LoginAdminAsync();
        await SetRoleAsync(admin.Token, student.UserId, role);
        return await LoginAsync(email);
    }

    protected async Task SetRoleAsync(string adminToken, Guid userId, string role)
    {
        using var client = CreateAuthenticatedClient(adminToken);
        using var response = await client.PutAsJsonAsync(
            $"/api/users/{userId}/role",
            new { role });
        response.EnsureSuccessStatusCode();
    }

    protected async Task<Guid> SubmitApplicationAsync(string token)
    {
        using var client = CreateAuthenticatedClient(token);
        using var response = await client.PostAsJsonAsync(
            "/api/organizer-applications",
            ApplicationPayload());
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await ReadJsonAsync(response)).GetProperty("id").GetGuid();
    }

    protected async Task<Guid> CreateEventAsync(
        string token,
        string title,
        int capacity,
        long priceMinor = 0)
    {
        using var client = CreateAuthenticatedClient(token);
        using var response = await client.PostAsJsonAsync(
            "/api/events",
            EventPayload(title, capacity, priceMinor));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await ReadJsonAsync(response)).GetProperty("id").GetGuid();
    }

    protected async Task<Guid> RegisterForEventAsync(string token, Guid eventId)
    {
        using var client = CreateAuthenticatedClient(token);
        using var response = await client.PostAsync($"/api/events/{eventId}/register", null);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await ReadJsonAsync(response)).GetProperty("registrationId").GetGuid();
    }

    protected HttpClient CreateAuthenticatedClient(string token)
    {
        var client = Fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    protected static object EventPayload(string title, int capacity, long priceMinor = 0)
    {
        var eventDate = DateTimeOffset.UtcNow.AddDays(7);
        return new
        {
        title,
        description = "A sufficiently detailed integration-test event description.",
        date = eventDate,
        location = "Integration Test Hall",
        capacity,
        category = "Technology",
        priceMinor,
        currency = "GHS",
        salesStartsAt = priceMinor > 0 ? DateTimeOffset.UtcNow : (DateTimeOffset?)null,
        salesEndsAt = priceMinor > 0 ? eventDate.AddHours(-1) : (DateTimeOffset?)null
        };
    }

    protected static object ApplicationPayload() => new
    {
        reason = "I want to organize useful events for the campus community."
    };

    protected static MultipartFormDataContent CreatePngUpload()
    {
        var pngHeader = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        var file = new ByteArrayContent(pngHeader);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        var form = new MultipartFormDataContent();
        form.Add(file, "file", "test.png");
        return form;
    }

    protected static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        return document.RootElement.Clone();
    }

    protected static async Task<HttpResponseMessage> SendAfterGateAsync(
        Task gate,
        Func<Task<HttpResponseMessage>> send)
    {
        await gate;
        return await send();
    }

    protected static string CreateExpiredToken(string validToken)
    {
        var handler = new JwtSecurityTokenHandler();
        var source = handler.ReadJwtToken(validToken);
        var claims = source.Claims.Where(claim =>
            claim.Type is not JwtRegisteredClaimNames.Exp and
            not JwtRegisteredClaimNames.Nbf and
            not JwtRegisteredClaimNames.Iat);
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ApiWebApplicationFactory.JwtSigningKey)),
            SecurityAlgorithms.HmacSha256);
        return handler.WriteToken(new JwtSecurityToken(
            source.Issuer,
            source.Audiences.Single(),
            claims,
            notBefore: DateTime.UtcNow.AddMinutes(-10),
            expires: DateTime.UtcNow.AddMinutes(-5),
            signingCredentials: credentials));
    }

    protected static ClaimsPrincipal ValidateToken(string token)
    {
        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        return handler.ValidateToken(token, new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "EventManagement.Api.IntegrationTests",
            ValidAudience = "EventManagement.Api.IntegrationTests.Client",
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(ApiWebApplicationFactory.JwtSigningKey)),
            RoleClaimType = JwtClaimNames.Role,
            ClockSkew = TimeSpan.Zero
        }, out _);
    }

    private static TestSession ReadSession(JsonElement body) => new(
        body.GetProperty("token").GetString()
            ?? throw new InvalidOperationException("The API returned no token."),
        body.GetProperty("user").GetProperty("id").GetGuid());

    protected sealed record TestSession(string Token, Guid UserId);
}
