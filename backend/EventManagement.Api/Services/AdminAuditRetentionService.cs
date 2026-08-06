using EventManagement.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Api.Services;

public sealed class AdminAuditRetentionService(
    AppDbContext dbContext,
    IConfiguration configuration,
    TimeProvider timeProvider)
{
    public async Task<int> ApplyAsync(CancellationToken cancellationToken)
    {
        var retentionDays = Math.Max(
            configuration.GetValue("DataRetention:AdminAuditLogs:RetentionDays", 365),
            30);
        var cutoff = timeProvider.GetUtcNow().AddDays(-retentionDays);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync(
            "SET LOCAL app.audit_retention_cleanup = 'on'", cancellationToken);
        var removed = await dbContext.AdminAuditLogs
            .Where(log => log.CreatedAt < cutoff)
            .ExecuteDeleteAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return removed;
    }
}
