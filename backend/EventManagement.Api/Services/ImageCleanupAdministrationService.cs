using EventManagement.Api.Data;
using EventManagement.Api.DTOs.Common;
using EventManagement.Api.DTOs.Images;
using EventManagement.Api.Infrastructure;
using EventManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Api.Services;

public sealed class ImageCleanupAdministrationService(
    AppDbContext dbContext,
    AdminAuditService auditService,
    TimeProvider timeProvider)
{
    public async Task<PaginatedResponse<FailedImageCleanupResponse>> GetFailedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        (page, pageSize) = Pagination.Normalize(page, pageSize);
        var query = dbContext.ImageUploads.AsNoTracking()
            .Where(upload => upload.Status == ImageUploadStatus.Failed);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(upload => upload.CreatedAt)
            .ThenBy(upload => upload.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(upload => new FailedImageCleanupResponse(
                upload.Id,
                upload.Bucket,
                upload.ObjectKey,
                upload.Kind.ToString(),
                upload.DeleteAttemptCount,
                upload.LifetimeDeleteAttemptCount,
                upload.ManualRetryCount,
                upload.LastRetriedAt,
                upload.LastError,
                upload.CreatedAt))
            .ToListAsync(cancellationToken);
        return new PaginatedResponse<FailedImageCleanupResponse>(
            items, page, pageSize, totalCount, Pagination.TotalPages(totalCount, pageSize));
    }

    public async Task RetryAsync(Guid id, Guid adminId, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var upload = await dbContext.ImageUploads
            .FromSqlInterpolated($"SELECT * FROM \"ImageUploads\" WHERE \"Id\" = {id} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Failed image cleanup item not found.");
        if (upload.Status != ImageUploadStatus.Failed)
            throw new ApiException(StatusCodes.Status409Conflict, "Only failed image cleanup items can be retried.");

        var previousAttemptCount = upload.DeleteAttemptCount;
        var now = timeProvider.GetUtcNow();
        upload.Status = ImageUploadStatus.DeletePending;
        upload.DeleteAttemptCount = 0;
        upload.ManualRetryCount += 1;
        upload.LastRetriedAt = now;
        upload.AvailableAt = now;
        upload.DeletionClaimedAt = null;
        upload.DeletionClaimedBy = null;
        upload.LastError = null;
        auditService.Append(adminId, "ImageCleanupRetried", "ImageUpload", upload.Id,
            new { upload.Bucket, upload.ObjectKey, PreviousAttemptCount = previousAttemptCount });
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
