using System.Data;
using System.Security.Cryptography;
using System.Text;
using EventManagement.Api.Data;
using EventManagement.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EventManagement.Api.Services;

public enum AuthRateLimitOperation
{
    Login,
    Registration,
    GoogleLogin,
    ForgotPassword,
    ResetPassword,
    ImageUpload
}

public interface IAuthRateLimitService
{
    ValueTask EnsureIpAllowedAsync(
        AuthRateLimitOperation operation,
        string address,
        CancellationToken cancellationToken);

    ValueTask EnsureAccountAllowedAsync(
        AuthRateLimitOperation operation,
        string accountKey,
        CancellationToken cancellationToken);

    ValueTask EnsureImageUploadAllowedAsync(
        Guid userId,
        CancellationToken cancellationToken);
}

public sealed class AuthRateLimitService(
    AppDbContext dbContext,
    IConfiguration configuration,
    TimeProvider timeProvider) : IAuthRateLimitService
{
    private static long _lastCleanupTick;

    public ValueTask EnsureIpAllowedAsync(
        AuthRateLimitOperation operation,
        string address,
        CancellationToken cancellationToken) =>
        EnsureAllowedAsync("Ip", operation, address, cancellationToken);

    public ValueTask EnsureAccountAllowedAsync(
        AuthRateLimitOperation operation,
        string accountKey,
        CancellationToken cancellationToken) =>
        EnsureAllowedAsync("Account", operation, accountKey, cancellationToken);

    public ValueTask EnsureImageUploadAllowedAsync(
        Guid userId,
        CancellationToken cancellationToken) =>
        EnsureAllowedAsync("Image", AuthRateLimitOperation.ImageUpload, userId.ToString(), cancellationToken);

    private async ValueTask EnsureAllowedAsync(
        string scope,
        AuthRateLimitOperation operation,
        string discriminator,
        CancellationToken cancellationToken)
    {
        var settings = GetSettings(scope, operation);
        var normalized = discriminator.Trim().ToLowerInvariant();
        var keyHash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
        var bucketKey = $"{scope}:{operation}:{keyHash}";
        var now = timeProvider.GetUtcNow();
        var cutoff = now - settings.Window;
        var connection = (NpgsqlConnection)dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose) await connection.OpenAsync(cancellationToken);
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText =
                """
                INSERT INTO "AuthRateLimitBuckets"
                    ("Key", "WindowStartedAt", "AttemptCount", "UpdatedAt")
                VALUES (@key, @now, 1, @now)
                ON CONFLICT ("Key") DO UPDATE
                SET "WindowStartedAt" = CASE
                        WHEN "AuthRateLimitBuckets"."WindowStartedAt" <= @cutoff THEN @now
                        ELSE "AuthRateLimitBuckets"."WindowStartedAt"
                    END,
                    "AttemptCount" = CASE
                        WHEN "AuthRateLimitBuckets"."WindowStartedAt" <= @cutoff THEN 1
                        ELSE "AuthRateLimitBuckets"."AttemptCount" + 1
                    END,
                    "UpdatedAt" = @now
                RETURNING "AttemptCount";
                """;
            command.Parameters.AddWithValue("key", bucketKey);
            command.Parameters.AddWithValue("now", now);
            command.Parameters.AddWithValue("cutoff", cutoff);
            var attemptCount = (int)(await command.ExecuteScalarAsync(cancellationToken)
                ?? throw new InvalidOperationException("The rate-limit counter returned no value."));
            if (attemptCount > settings.PermitLimit)
                throw new ApiException(
                    StatusCodes.Status429TooManyRequests,
                    scope == "Ip"
                        ? "Too many authentication attempts from this network. Please wait and try again."
                        : "Too many attempts for this account. Please wait and try again.");
        }
        finally
        {
            if (shouldClose) await connection.CloseAsync();
        }

        await TryCleanupExpiredBucketsAsync(now, cancellationToken);
    }

    private async Task TryCleanupExpiredBucketsAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var currentTick = Environment.TickCount64;
        var lastTick = Interlocked.Read(ref _lastCleanupTick);
        if (currentTick - lastTick < TimeSpan.FromHours(1).TotalMilliseconds ||
            Interlocked.CompareExchange(ref _lastCleanupTick, currentTick, lastTick) != lastTick)
            return;

        await dbContext.AuthRateLimitBuckets
            .Where(bucket => bucket.UpdatedAt < now.AddDays(-7))
            .ExecuteDeleteAsync(cancellationToken);
    }

    private RateLimitSettings GetSettings(string scope, AuthRateLimitOperation operation)
    {
        if (operation == AuthRateLimitOperation.ImageUpload)
            return new RateLimitSettings(
                Math.Max(configuration.GetValue("Images:UploadRateLimit:PermitLimit", 10), 1),
                TimeSpan.FromMinutes(Math.Max(
                    configuration.GetValue("Images:UploadRateLimit:WindowMinutes", 60),
                    1)));

        var sectionName = operation.ToString();
        var permitLimit = configuration.GetValue(
            $"AuthRateLimiting:{scope}:{sectionName}:PermitLimit",
            DefaultPermitLimit(scope, operation));
        var windowMinutes = configuration.GetValue(
            $"AuthRateLimiting:{scope}:{sectionName}:WindowMinutes",
            DefaultWindowMinutes(scope, operation));
        return new RateLimitSettings(
            Math.Max(permitLimit, 1),
            TimeSpan.FromMinutes(Math.Max(windowMinutes, 1)));
    }

    private static int DefaultPermitLimit(string scope, AuthRateLimitOperation operation) =>
                    scope == "Ip"
            ? operation switch
            {
                AuthRateLimitOperation.Login => 30,
                AuthRateLimitOperation.Registration => 10,
                AuthRateLimitOperation.GoogleLogin => 30,
                AuthRateLimitOperation.ForgotPassword => 10,
                AuthRateLimitOperation.ResetPassword => 20,
                _ => 10
            }
            : operation switch
            {
                AuthRateLimitOperation.Login => 8,
                AuthRateLimitOperation.Registration => 3,
                AuthRateLimitOperation.GoogleLogin => 20,
                AuthRateLimitOperation.ForgotPassword => 3,
                AuthRateLimitOperation.ResetPassword => 5,
                _ => 5
            };

    private static int DefaultWindowMinutes(string scope, AuthRateLimitOperation operation) =>
        scope == "Ip"
            ? operation is AuthRateLimitOperation.Login or AuthRateLimitOperation.GoogleLogin ? 5 : 60
            : operation switch
            {
                AuthRateLimitOperation.Registration => 24 * 60,
                AuthRateLimitOperation.ForgotPassword => 60,
                _ => 15
            };

    private sealed record RateLimitSettings(int PermitLimit, TimeSpan Window);
}
