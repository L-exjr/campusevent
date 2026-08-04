using EventManagement.Api.Data;
using EventManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Api.Services;

public sealed class EmailOutboxBackgroundService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<EmailOutboxBackgroundService> logger) : BackgroundService
{
    private readonly Guid _workerId = Guid.NewGuid();
    private DateTimeOffset _nextReminderScan = DateTimeOffset.MinValue;

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
            if (now >= _nextReminderScan)
            {
                await using var enqueueScope = scopeFactory.CreateAsyncScope();
                await enqueueScope.ServiceProvider.GetRequiredService<EventReminderEnqueuer>()
                    .EnqueueDueAsync(cancellationToken);
                _nextReminderScan = now.AddMinutes(Math.Max(
                    configuration.GetValue("Email:Reminders:CheckIntervalMinutes", 60),
                    1));
            }

            var messages = await ClaimBatchAsync(cancellationToken);
            foreach (var message in messages)
            {
                await using var processingScope = scopeFactory.CreateAsyncScope();
                await processingScope.ServiceProvider.GetRequiredService<EmailOutboxMessageProcessor>()
                    .ProcessAsync(message, _workerId, cancellationToken);
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

    private async Task<IReadOnlyList<EmailOutboxMessage>> ClaimBatchAsync(
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var staleBefore = now.AddMinutes(-Math.Max(
            configuration.GetValue("Email:Outbox:ClaimLeaseMinutes", 10),
            1));
        var batchSize = Math.Clamp(configuration.GetValue("Email:Outbox:BatchSize", 50), 1, 500);

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
                "ClaimedBy" = {_workerId},
                "ClaimedAt" = {now},
                "AttemptCount" = message."AttemptCount" + 1
            FROM candidates
            WHERE message."Id" = candidates."Id";
            """,
            cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await dbContext.EmailOutboxMessages.AsNoTracking()
            .Where(message => message.ClaimedBy == _workerId &&
                              message.Status == EmailOutboxStatus.Processing)
            .OrderBy(message => message.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
