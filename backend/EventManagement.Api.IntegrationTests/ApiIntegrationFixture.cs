using EventManagement.Api.Data;
using EventManagement.Api.Models;
using EventManagement.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
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
                "AuthRateLimitBuckets",
                "AdminAuditLogs",
                "EmailOutboxMessages",
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

    public async Task<int> CountEventsAsync()
    {
        await using var dbContext = CreateDbContext();
        return await dbContext.Events.CountAsync();
    }

    public async Task<int> CountEmailOutboxMessagesAsync(string kind)
    {
        await using var dbContext = CreateDbContext();
        return await dbContext.EmailOutboxMessages.CountAsync(message => message.Kind == kind);
    }

    public async Task<bool> EmailOutboxPayloadExistsAsync(string kind)
    {
        await using var dbContext = CreateDbContext();
        return await dbContext.EmailOutboxMessages.AnyAsync(
            message => message.Kind == kind && message.PayloadJson != null);
    }

    public async Task<Guid> CreateFailedEmailOutboxMessageAsync(string kind, string? payloadJson)
    {
        await using var dbContext = CreateDbContext();
        var message = new EmailOutboxMessage
        {
            IdempotencyKey = $"integration-failed:{Guid.NewGuid():N}",
            Kind = kind,
            AggregateId = Guid.NewGuid(),
            PayloadJson = payloadJson,
            Status = EmailOutboxStatus.Failed,
            AttemptCount = 8,
            LifetimeAttemptCount = 8,
            LastError = "Provider unavailable"
        };
        dbContext.EmailOutboxMessages.Add(message);
        await dbContext.SaveChangesAsync();
        return message.Id;
    }

    public async Task<(
        EmailOutboxStatus Status,
        int AttemptCount,
        int LifetimeAttemptCount,
        int ManualRetryCount)> GetEmailOutboxStateAsync(Guid id)
    {
        await using var dbContext = CreateDbContext();
        return await dbContext.EmailOutboxMessages
            .Where(message => message.Id == id)
            .Select(message => new ValueTuple<EmailOutboxStatus, int, int, int>(
                message.Status,
                message.AttemptCount,
                message.LifetimeAttemptCount,
                message.ManualRetryCount))
            .SingleAsync();
    }

    public async Task<Guid> CreateTerminalEmailOutboxMessageAsync(
        EmailOutboxStatus status,
        DateTimeOffset createdAt)
    {
        await using var dbContext = CreateDbContext();
        var message = new EmailOutboxMessage
        {
            IdempotencyKey = $"integration-terminal:{Guid.NewGuid():N}",
            Kind = EmailOutbox.EventReminderKind,
            AggregateId = Guid.NewGuid(),
            Status = status,
            CreatedAt = createdAt
        };
        dbContext.EmailOutboxMessages.Add(message);
        await dbContext.SaveChangesAsync();
        return message.Id;
    }

    public async Task<bool> EmailOutboxMessageExistsAsync(Guid id)
    {
        await using var dbContext = CreateDbContext();
        return await dbContext.EmailOutboxMessages.AnyAsync(message => message.Id == id);
    }

    public async Task ApplyEmailOutboxRetentionAsync()
    {
        await using var dbContext = CreateDbContext();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Email:Outbox:DeliveredRetentionDays"] = "30",
                ["Email:Outbox:FailedRetentionDays"] = "90"
            })
            .Build();
        await new EmailOutboxRetentionService(dbContext, configuration)
            .ApplyAsync(CancellationToken.None);
    }

    public async Task<bool> AdminAuditMutationIsRejectedAsync(Guid id)
    {
        await using var dbContext = CreateDbContext();
        try
        {
            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE \"AdminAuditLogs\" SET \"Action\" = 'Tampered' WHERE \"Id\" = {id}");
            return false;
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.RaiseException)
        {
            return true;
        }
    }

    public async Task SetAuthRateLimitCountAsync(
        string scope,
        string operation,
        string discriminator,
        int count)
    {
        var normalized = discriminator.Trim().ToLowerInvariant();
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
        await using var dbContext = CreateDbContext();
        dbContext.AuthRateLimitBuckets.Add(new AuthRateLimitBucket
        {
            Key = $"{scope}:{operation}:{hash}",
            WindowStartedAt = DateTimeOffset.UtcNow,
            AttemptCount = count,
            UpdatedAt = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync();
    }

    public async Task<(string? Url, string? ObjectKey)> GetUserImageStateAsync(Guid userId)
    {
        await using var dbContext = CreateDbContext();
        var state = await dbContext.Users
            .Where(user => user.Id == userId)
            .Select(user => new { user.ImageUrl, user.ImageObjectKey })
            .SingleAsync();
        return (state.ImageUrl, state.ImageObjectKey);
    }

    public async Task<ImageUploadStatus> GetImageUploadStatusAsync(string objectKey)
    {
        await using var dbContext = CreateDbContext();
        return await dbContext.ImageUploads
            .Where(upload => upload.ObjectKey == objectKey)
            .Select(upload => upload.Status)
            .SingleAsync();
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

    public async Task SetEventDateAsync(Guid eventId, DateTimeOffset date)
    {
        await using var dbContext = CreateDbContext();
        var item = await dbContext.Events.SingleAsync(eventEntity => eventEntity.Id == eventId);
        item.Date = date;
        await dbContext.SaveChangesAsync();
    }

    private AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_database.ConnectionString)
            .Options;
        return new AppDbContext(options);
    }
}
