using EventManagement.Api.Data;
using EventManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Api.Services;

public sealed class EmailOutboxBackgroundService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<EmailOutboxBackgroundService> logger) : BackgroundService
{
    private DateTimeOffset _nextReminderScan = DateTimeOffset.MinValue;
    private DateTimeOffset _nextRetentionSweep = DateTimeOffset.MinValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RunCycleAsync(stoppingToken);
        var interval = TimeSpan.FromSeconds(Math.Max(
            configuration.GetValue("Email:Outbox:PollIntervalSeconds", 15),
            5));
        using var timer = new PeriodicTimer(interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
            await RunCycleAsync(stoppingToken);
    }

    private async Task RunCycleAsync(CancellationToken cancellationToken)
    {
        try
        {
            var now = DateTimeOffset.UtcNow;
            if (now >= _nextRetentionSweep)
            {
                try
                {
                    await using var retentionScope = scopeFactory.CreateAsyncScope();
                    await retentionScope.ServiceProvider.GetRequiredService<EmailOutboxRetentionService>()
                        .ApplyAsync(cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Email outbox retention sweep failed; delivery will continue.");
                }
                _nextRetentionSweep = now.AddMinutes(Math.Max(
                    configuration.GetValue("Email:Outbox:RetentionSweepMinutes", 60),
                    1));
            }
            if (now >= _nextReminderScan)
            {
                try
                {
                    await using var enqueueScope = scopeFactory.CreateAsyncScope();
                    await enqueueScope.ServiceProvider.GetRequiredService<EventReminderEnqueuer>()
                        .EnqueueDueAsync(cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Event reminder scan failed; queued email delivery will continue.");
                }
                _nextReminderScan = now.AddMinutes(Math.Max(
                    configuration.GetValue("Email:Reminders:CheckIntervalMinutes", 60),
                    1));
            }

            var batch = await ClaimBatchAsync(cancellationToken);
            foreach (var message in batch.Messages)
            {
                try
                {
                    await using var processingScope = scopeFactory.CreateAsyncScope();
                    await processingScope.ServiceProvider.GetRequiredService<EmailOutboxMessageProcessor>()
                        .ProcessAsync(message, batch.ClaimId, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    logger.LogError(
                        exception,
                        "Email outbox message {MessageId} failed unexpectedly; continuing the batch.",
                        message.Id);
                    try
                    {
                        await using var recoveryScope = scopeFactory.CreateAsyncScope();
                        await recoveryScope.ServiceProvider
                            .GetRequiredService<EmailOutboxMessageProcessor>()
                            .RecoverUnexpectedFailureAsync(
                                message.Id,
                                batch.ClaimId,
                                exception,
                                cancellationToken);
                    }
                    catch (Exception recoveryException)
                    {
                        logger.LogError(
                            recoveryException,
                            "Could not release failed email outbox claim {MessageId}; the lease will expire.",
                            message.Id);
                    }
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Normal host shutdown.
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "The email outbox cycle failed; it will retry later.");
        }
    }

    private async Task<ClaimedEmailBatch> ClaimBatchAsync(
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var staleBefore = now.AddMinutes(-Math.Max(
            configuration.GetValue("Email:Outbox:ClaimLeaseMinutes", 10),
            1));
        var batchSize = Math.Clamp(configuration.GetValue("Email:Outbox:BatchSize", 50), 1, 500);
        var claimId = Guid.NewGuid();

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            WITH candidates AS (
                SELECT "Id"
                FROM "EmailOutboxMessages"
                WHERE ("Status" = 'Pending' AND "AvailableAt" <= {now})
                   OR ("Status" = 'Processing' AND "ClaimedAt" < {staleBefore})
                ORDER BY "AvailableAt", "CreatedAt"
                FOR UPDATE SKIP LOCKED
                LIMIT {batchSize}
            )
            UPDATE "EmailOutboxMessages" AS message
            SET "Status" = 'Processing',
                "ClaimedBy" = {claimId},
                "ClaimedAt" = {now},
                "AttemptCount" = message."AttemptCount" + 1,
                "LifetimeAttemptCount" = message."LifetimeAttemptCount" + 1
            FROM candidates
            WHERE message."Id" = candidates."Id";
            """,
            cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var messages = await dbContext.EmailOutboxMessages.AsNoTracking()
            .Where(message => message.ClaimedBy == claimId &&
                              message.Status == EmailOutboxStatus.Processing)
            .OrderBy(message => message.CreatedAt)
            .ToListAsync(cancellationToken);
        return new ClaimedEmailBatch(claimId, messages);
    }

    private sealed record ClaimedEmailBatch(
        Guid ClaimId,
        IReadOnlyList<EmailOutboxMessage> Messages);
}
