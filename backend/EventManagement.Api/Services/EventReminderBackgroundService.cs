using EventManagement.Api.Data;
using EventManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Api.Services;

public sealed class EventReminderBackgroundService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<EventReminderBackgroundService> logger) : BackgroundService
{
    private const string ReminderKind = "EventReminder";
    private readonly Guid _workerId = Guid.NewGuid();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RunCycleAsync(stoppingToken);
        var interval = TimeSpan.FromMinutes(Math.Max(
            configuration.GetValue("Email:Reminders:CheckIntervalMinutes", 60),
            1));
        using var timer = new PeriodicTimer(interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
            await RunCycleAsync(stoppingToken);
    }

    private async Task RunCycleAsync(CancellationToken cancellationToken)
    {
        try
        {
            await EnqueueDueRemindersAsync(cancellationToken);
            var messages = await ClaimBatchAsync(cancellationToken);
            foreach (var message in messages)
                await ProcessAsync(message, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Normal host shutdown.
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "The event reminder outbox cycle failed; it will retry later.");
        }
    }

    private async Task EnqueueDueRemindersAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var reminderCutoff = now.AddHours(Math.Max(
            configuration.GetValue("Email:Reminders:LeadTimeHours", 24),
            1));

        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO "EmailOutboxMessages"
                ("Id", "IdempotencyKey", "Kind", "AggregateId", "Status",
                 "AvailableAt", "AttemptCount", "CreatedAt")
            SELECT registration."Id",
                   'event-reminder:' || registration."Id"::text,
                   {ReminderKind},
                   registration."Id",
                   'Pending',
                   {now},
                   0,
                   {now}
            FROM "EventRegistrations" AS registration
            INNER JOIN "Events" AS event_entity
                ON event_entity."Id" = registration."EventId"
            WHERE registration."ReminderSentAt" IS NULL
              AND event_entity."Date" > {now}
              AND event_entity."Date" <= {reminderCutoff}
            ON CONFLICT ("IdempotencyKey") DO NOTHING;
            """,
            cancellationToken);
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
        var batchSize = Math.Clamp(
            configuration.GetValue("Email:Outbox:BatchSize", 50),
            1,
            500);

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
            .Where(message =>
                message.ClaimedBy == _workerId &&
                message.Status == EmailOutboxStatus.Processing)
            .OrderBy(message => message.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    private async Task ProcessAsync(
        EmailOutboxMessage message,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var registration = await dbContext.EventRegistrations.AsNoTracking()
            .Include(item => item.Event)
            .Include(item => item.Student)
            .SingleOrDefaultAsync(item => item.Id == message.AggregateId, cancellationToken);

        if (registration is null || registration.ReminderSentAt.HasValue)
        {
            await FinishAsync(
                dbContext,
                message,
                EmailOutboxStatus.Discarded,
                "The registration no longer requires a reminder.",
                cancellationToken);
            return;
        }

        var sent = await emailService.SendEmailAsync(
            registration.Student.Email,
            registration.Student.Name,
            $"Reminder: {registration.Event.Title} starts soon",
            "EventReminder.html",
            new Dictionary<string, string?>
            {
                ["StudentName"] = registration.Student.Name,
                ["EventTitle"] = registration.Event.Title,
                ["EventDate"] = registration.Event.Date.ToString("f"),
                ["EventLocation"] = registration.Event.Location
            },
            cancellationToken);

        if (!sent)
        {
            await ReleaseForRetryAsync(dbContext, message, cancellationToken);
            return;
        }

        var trackedRegistration = await dbContext.EventRegistrations.SingleOrDefaultAsync(
            item => item.Id == registration.Id,
            cancellationToken);
        if (trackedRegistration is not null && !trackedRegistration.ReminderSentAt.HasValue)
            trackedRegistration.ReminderSentAt = DateTimeOffset.UtcNow;
        await FinishAsync(dbContext, message, EmailOutboxStatus.Sent, null, cancellationToken);
    }

    private async Task ReleaseForRetryAsync(
        AppDbContext dbContext,
        EmailOutboxMessage claimedMessage,
        CancellationToken cancellationToken)
    {
        var message = await GetOwnedClaimAsync(dbContext, claimedMessage.Id, cancellationToken);
        var maxAttempts = Math.Max(configuration.GetValue("Email:Outbox:MaxAttempts", 8), 1);
        message.Status = message.AttemptCount >= maxAttempts
            ? EmailOutboxStatus.Failed
            : EmailOutboxStatus.Pending;
        message.AvailableAt = DateTimeOffset.UtcNow.AddMinutes(
            Math.Min(Math.Pow(2, Math.Max(message.AttemptCount - 1, 0)), 60));
        message.LastError = "The email provider did not accept the message.";
        message.ClaimedAt = null;
        message.ClaimedBy = null;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task FinishAsync(
        AppDbContext dbContext,
        EmailOutboxMessage claimedMessage,
        EmailOutboxStatus status,
        string? lastError,
        CancellationToken cancellationToken)
    {
        var message = await GetOwnedClaimAsync(dbContext, claimedMessage.Id, cancellationToken);
        message.Status = status;
        message.SentAt = status == EmailOutboxStatus.Sent ? DateTimeOffset.UtcNow : null;
        message.LastError = lastError;
        message.ClaimedAt = null;
        message.ClaimedBy = null;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<EmailOutboxMessage> GetOwnedClaimAsync(
        AppDbContext dbContext,
        Guid messageId,
        CancellationToken cancellationToken) =>
        await dbContext.EmailOutboxMessages.SingleAsync(
            message =>
                message.Id == messageId &&
                message.ClaimedBy == _workerId &&
                message.Status == EmailOutboxStatus.Processing,
            cancellationToken);
}
