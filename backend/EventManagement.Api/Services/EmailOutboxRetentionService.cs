using EventManagement.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Api.Services;

public sealed class EmailOutboxRetentionService(
    AppDbContext dbContext,
    IConfiguration configuration,
    TimeProvider timeProvider)
{
    public async Task ApplyAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var deliveredBefore = now.AddDays(-Math.Max(
            configuration.GetValue("Email:Outbox:DeliveredRetentionDays", 30),
            1));
        var failedBefore = now.AddDays(-Math.Max(
            configuration.GetValue("Email:Outbox:FailedRetentionDays", 90),
            1));
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE "EmailOutboxMessages" AS message
            SET "PayloadJson" = NULL
            WHERE message."Kind" = {EmailOutbox.PasswordResetKind}
              AND message."PayloadJson" IS NOT NULL
              AND NOT EXISTS (
                  SELECT 1
                  FROM "PasswordResetTokens" AS token
                  INNER JOIN "Users" AS account ON account."Id" = token."UserId"
                  WHERE token."Id" = message."AggregateId"
                    AND token."UsedAt" IS NULL
                    AND token."ExpiresAt" > {now}
                    AND account."IsActive"
              );

            DELETE FROM "EmailOutboxMessages"
            WHERE ("Status" IN ('Sent', 'Discarded') AND "CreatedAt" < {deliveredBefore})
               OR ("Status" = 'Failed' AND "CreatedAt" < {failedBefore});
            """,
            cancellationToken);
    }
}
