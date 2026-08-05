using EventManagement.Api.Data;
using EventManagement.Api.DTOs.Common;
using EventManagement.Api.DTOs.EmailOutbox;
using EventManagement.Api.Infrastructure;
using EventManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Api.Services;

public sealed class EmailOutboxAdministrationService(
    AppDbContext dbContext,
    AdminAuditService auditService)
{
    public async Task<PaginatedResponse<FailedEmailOutboxResponse>> GetFailedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        (page, pageSize) = Pagination.Normalize(page, pageSize);
        var query = dbContext.EmailOutboxMessages.AsNoTracking()
            .Where(message => message.Status == EmailOutboxStatus.Failed);
        var totalCount = await query.CountAsync(cancellationToken);
        var messages = await query
            .OrderByDescending(message => message.CreatedAt)
            .ThenBy(message => message.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(message => new FailedEmailOutboxResponse(
                message.Id,
                message.Kind,
                message.AggregateId,
                message.AttemptCount,
                message.LifetimeAttemptCount,
                message.ManualRetryCount,
                message.LastRetriedAt,
                message.LastError,
                message.CreatedAt,
                EmailOutboxRecoveryPolicy.CanRetry(message.Kind)))
            .ToListAsync(cancellationToken);
        return new PaginatedResponse<FailedEmailOutboxResponse>(
            messages,
            page,
            pageSize,
            totalCount,
            Pagination.TotalPages(totalCount, pageSize));
    }

    public async Task RetryAsync(
        Guid messageId,
        Guid adminId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var message = await dbContext.EmailOutboxMessages
            .FromSqlInterpolated(
                $"SELECT * FROM \"EmailOutboxMessages\" WHERE \"Id\" = {messageId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Failed email message not found.");
        if (message.Status != EmailOutboxStatus.Failed)
            throw new ApiException(StatusCodes.Status409Conflict, "Only failed email messages can be retried.");
        if (!EmailOutboxRecoveryPolicy.CanRetry(message.Kind))
            throw new ApiException(
                StatusCodes.Status409Conflict,
                "This message cannot be retried safely. Generate a new domain action instead.");

        var previousAttemptCount = message.AttemptCount;
        message.Status = EmailOutboxStatus.Pending;
        message.AttemptCount = 0;
        message.ManualRetryCount += 1;
        message.LastRetriedAt = DateTimeOffset.UtcNow;
        message.AvailableAt = DateTimeOffset.UtcNow;
        message.ClaimedAt = null;
        message.ClaimedBy = null;
        message.LastError = null;
        auditService.Append(
            adminId,
            "EmailDeadLetterRetried",
            "EmailOutboxMessage",
            message.Id,
            new { message.Kind, PreviousAttemptCount = previousAttemptCount });
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
