using EventManagement.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Api.Services;

public sealed class EventReminderEnqueuer(AppDbContext dbContext, IConfiguration configuration)
{
    public async Task EnqueueDueAsync(CancellationToken cancellationToken)
    {
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
                   {EmailOutbox.EventReminderKind},
                   registration."Id",
                   'Pending',
                   {now},
                   0,
                   {now}
            FROM "EventRegistrations" AS registration
            INNER JOIN "Events" AS event_entity ON event_entity."Id" = registration."EventId"
            INNER JOIN "Users" AS student ON student."Id" = registration."StudentId"
            WHERE registration."ReminderSentAt" IS NULL
              AND event_entity."IsPublished"
              AND student."IsActive"
              AND event_entity."Date" > {now}
              AND event_entity."Date" <= {reminderCutoff}
            ON CONFLICT ("IdempotencyKey") DO NOTHING;
            """,
            cancellationToken);
    }
}
