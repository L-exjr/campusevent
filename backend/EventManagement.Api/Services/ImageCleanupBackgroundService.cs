using EventManagement.Api.Data;
using EventManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Api.Services;

public sealed class ImageCleanupBackgroundService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<ImageCleanupBackgroundService> logger,
    TimeProvider timeProvider,
    OperationalMetrics metrics) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RunCycleAsync(stoppingToken);
        var interval = TimeSpan.FromMinutes(Math.Max(
            configuration.GetValue("Images:Cleanup:IntervalMinutes", 60),
            1));
        using var timer = new PeriodicTimer(interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
            await RunCycleAsync(stoppingToken);
    }

    private async Task RunCycleAsync(CancellationToken cancellationToken)
    {
        try
        {
            var batch = await ClaimBatchAsync(cancellationToken);
            foreach (var upload in batch.Uploads)
            {
                try
                {
                    await DeleteAsync(upload, batch.ClaimId, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    logger.LogError(
                        exception,
                        "Image cleanup item {UploadId} failed unexpectedly; continuing the batch.",
                        upload.Id);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Normal host shutdown.
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "The image cleanup cycle failed; it will retry later.");
        }
    }

    private async Task<ClaimedImageBatch> ClaimBatchAsync(
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = timeProvider.GetUtcNow();
        var pendingCutoff = now.AddHours(-Math.Max(
            configuration.GetValue("Images:Cleanup:PendingRetentionHours", 24),
            1));
        var staleBefore = now.AddMinutes(-Math.Max(
            configuration.GetValue("Images:Cleanup:ClaimLeaseMinutes", 10),
            1));
        var batchSize = Math.Clamp(
            configuration.GetValue("Images:Cleanup:BatchSize", 50),
            1,
            500);
        var claimId = Guid.NewGuid();

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE "ImageUploads"
            SET "Status" = 'DeletePending', "AvailableAt" = {now}
            WHERE "Status" = 'Pending' AND "CreatedAt" < {pendingCutoff};

            WITH candidates AS (
                SELECT "Id"
                FROM "ImageUploads"
                WHERE ("Status" = 'DeletePending' AND "AvailableAt" <= {now})
                   OR ("Status" = 'Deleting' AND "DeletionClaimedAt" < {staleBefore})
                ORDER BY "AvailableAt", "CreatedAt"
                FOR UPDATE SKIP LOCKED
                LIMIT {batchSize}
            )
            UPDATE "ImageUploads" AS upload
            SET "Status" = 'Deleting',
                "DeletionClaimedBy" = {claimId},
                "DeletionClaimedAt" = {now},
                "DeleteAttemptCount" = upload."DeleteAttemptCount" + 1,
                "LifetimeDeleteAttemptCount" = upload."LifetimeDeleteAttemptCount" + 1
            FROM candidates
            WHERE upload."Id" = candidates."Id";
            """,
            cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var uploads = await dbContext.ImageUploads.AsNoTracking()
            .Where(upload =>
                upload.DeletionClaimedBy == claimId &&
                upload.Status == ImageUploadStatus.Deleting)
            .OrderBy(upload => upload.CreatedAt)
            .ToListAsync(cancellationToken);
        return new ClaimedImageBatch(claimId, uploads);
    }

    private async Task DeleteAsync(
        ImageUpload claimedUpload,
        Guid claimId,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var storageService = scope.ServiceProvider.GetRequiredService<IImageStorageService>();
        try
        {
            await storageService.DeleteImageAsync(
                claimedUpload.Bucket,
                claimedUpload.ObjectKey,
                cancellationToken);
            var upload = await GetOwnedClaimAsync(
                dbContext,
                claimedUpload.Id,
                claimId,
                cancellationToken);
            upload.Status = ImageUploadStatus.Deleted;
            upload.DeletedAt = timeProvider.GetUtcNow();
            upload.DeletionClaimedAt = null;
            upload.DeletionClaimedBy = null;
            upload.LastError = null;
            await dbContext.SaveChangesAsync(cancellationToken);
            metrics.StorageCleanup(true);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Deletion failed for image {ObjectKey} in {Bucket}.",
                claimedUpload.ObjectKey,
                claimedUpload.Bucket);
            var upload = await GetOwnedClaimAsync(
                dbContext,
                claimedUpload.Id,
                claimId,
                cancellationToken);
            var maxAttempts = Math.Max(
                configuration.GetValue("Images:Cleanup:MaxAttempts", 8),
                1);
            upload.Status = upload.DeleteAttemptCount >= maxAttempts
                ? ImageUploadStatus.Failed
                : ImageUploadStatus.DeletePending;
            upload.AvailableAt = timeProvider.GetUtcNow().AddMinutes(
                Math.Min(Math.Pow(2, Math.Max(upload.DeleteAttemptCount - 1, 0)), 60));
            upload.LastError = exception.Message[..Math.Min(exception.Message.Length, 2000)];
            upload.DeletionClaimedAt = null;
            upload.DeletionClaimedBy = null;
            await dbContext.SaveChangesAsync(cancellationToken);
            metrics.StorageCleanup(false);
        }
    }

    private async Task<ImageUpload> GetOwnedClaimAsync(
        AppDbContext dbContext,
        Guid uploadId,
        Guid claimId,
        CancellationToken cancellationToken) =>
        await dbContext.ImageUploads.SingleAsync(
            upload =>
                upload.Id == uploadId &&
                upload.DeletionClaimedBy == claimId &&
                upload.Status == ImageUploadStatus.Deleting,
            cancellationToken);

    private sealed record ClaimedImageBatch(
        Guid ClaimId,
        IReadOnlyList<ImageUpload> Uploads);
}
