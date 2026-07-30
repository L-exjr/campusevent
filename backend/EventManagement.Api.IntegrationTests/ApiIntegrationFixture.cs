using EventManagement.Api.Data;
using EventManagement.Api.Models;
using EventManagement.Api.Services;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace EventManagement.Api.IntegrationTests;

public sealed class ApiIntegrationFixture : IAsyncLifetime
{
    public const string AdminEmail = "admin.integration@example.test";
    public const string AdminPassword = "Admin-Integration-Password-123!";

    private PostgresTestDatabase _database = null!;
    private ApiWebApplicationFactory _factory = null!;

    public HttpClient CreateClient() => _factory.CreateClient();

    public async Task InitializeAsync()
    {
        _database = await PostgresTestDatabase.StartAsync();
        await using (var dbContext = CreateDbContext())
        {
            await dbContext.Database.MigrateAsync();
        }

        _factory = new ApiWebApplicationFactory(_database.ConnectionString);
        using var client = _factory.CreateClient();
        using var response = await client.GetAsync("/api/events");
        response.EnsureSuccessStatusCode();
    }

    public async Task DisposeAsync()
    {
        _factory.Dispose();
        await _database.DisposeAsync();
    }

    public async Task ResetAsync()
    {
        await using var dbContext = CreateDbContext();
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            TRUNCATE TABLE
                "EventRegistrations",
                "OrganizerApplications",
                "PasswordResetTokens",
                "BookingRequests",
                "Events",
                "Users"
            CASCADE;
            """);

        var hasher = new Pbkdf2PasswordHasher();
        dbContext.Users.Add(new User
        {
            Name = "Integration Test Admin",
            Email = AdminEmail,
            PasswordHash = hasher.Hash(AdminPassword),
            Role = UserRole.Admin,
            IsActive = true
        });
        await dbContext.SaveChangesAsync();
    }

    public async Task<int> CountRegistrationsAsync(Guid eventId)
    {
        await using var dbContext = CreateDbContext();
        return await dbContext.EventRegistrations.CountAsync(
            registration => registration.EventId == eventId);
    }

    public async Task<int> CountPendingApplicationsAsync(Guid userId)
    {
        await using var dbContext = CreateDbContext();
        return await dbContext.OrganizerApplications.CountAsync(application =>
            application.UserId == userId && application.Status == ApplicationStatus.Pending);
    }

    public async Task<int> CountUsersByEmailAsync(string email)
    {
        await using var dbContext = CreateDbContext();
        return await dbContext.Users.CountAsync(user => user.Email == email);
    }

    public async Task<int> CountBookingRequestsAsync()
    {
        await using var dbContext = CreateDbContext();
        return await dbContext.BookingRequests.CountAsync();
    }

    public async Task<string> CreateResetTokenAsync(Guid userId, DateTimeOffset expiresAt)
    {
        const string rawToken = "integration-reset-token";
        await using var dbContext = CreateDbContext();
        dbContext.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId = userId,
            TokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken))).ToLowerInvariant(),
            ExpiresAt = expiresAt
        });
        await dbContext.SaveChangesAsync();
        return rawToken;
    }

    public async Task<(bool IsPublished, Guid OrganizerId)> GetEventStateAsync(Guid eventId)
    {
        await using var dbContext = CreateDbContext();
        var item = await dbContext.Events.SingleAsync(eventEntity => eventEntity.Id == eventId);
        return (item.IsPublished, item.OrganizerId);
    }

    private AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_database.ConnectionString)
            .Options;
        return new AppDbContext(options);
    }
}
