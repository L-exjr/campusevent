using EventManagement.Api.Data;
using EventManagement.Api.Infrastructure;
using EventManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Api.Services;

public interface IImageLifecycleService
{
    Task<ImageUpload> CreatePendingAsync(
        Stream content,
        string contentType,
        string bucket,
        string extension,
        Guid ownerId,
        ImageUploadKind kind,
        CancellationToken cancellationToken);

    Task<ClaimedImage> ClaimAsync(
        Guid ownerId,
        ImageUploadKind kind,
        string? requestedUrl,
        string? currentUrl,
        string? currentObjectKey,
        CancellationToken cancellationToken);

    Task MarkForDeletionAsync(string? objectKey, CancellationToken cancellationToken);
}

public sealed record ClaimedImage(string? Url, string? ObjectKey);

public sealed class ImageLifecycleService(
    AppDbContext dbContext,
    IImageStorageService storageService,
    ILogger<ImageLifecycleService> logger) : IImageLifecycleService
{
    public async Task<ImageUpload> CreatePendingAsync(
        Stream content,
        string contentType,
        string bucket,
        string extension,
        Guid ownerId,
        ImageUploadKind kind,
        CancellationToken cancellationToken)
    {
        var stored = await storageService.UploadImageAsync(
            content,
            contentType,
            bucket,
            extension,
            ownerId,
            cancellationToken);
        var upload = new ImageUpload
        {
            OwnerId = ownerId,
            Bucket = bucket,
            ObjectKey = stored.ObjectKey,
            PublicUrl = stored.PublicUrl,
            Kind = kind,
            Status = ImageUploadStatus.Pending
        };
        dbContext.ImageUploads.Add(upload);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return upload;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Image {ObjectKey} was stored, but its pending database record failed.",
                stored.ObjectKey);
            try
            {
                await storageService.DeleteImageAsync(bucket, stored.ObjectKey, CancellationToken.None);
            }
            catch (Exception cleanupException)
            {
                logger.LogCritical(
                    cleanupException,
                    "Compensating deletion failed for untracked image {ObjectKey} in {Bucket}.",
                    stored.ObjectKey,
                    bucket);
            }
            throw;
        }
    }

    public async Task<ClaimedImage> ClaimAsync(
        Guid ownerId,
        ImageUploadKind kind,
        string? requestedUrl,
        string? currentUrl,
        string? currentObjectKey,
        CancellationToken cancellationToken)
    {
        var normalizedUrl = string.IsNullOrWhiteSpace(requestedUrl) ? null : requestedUrl.Trim();
        if (string.Equals(normalizedUrl, currentUrl, StringComparison.Ordinal))
            return new ClaimedImage(currentUrl, currentObjectKey);

        ImageUpload? upload = null;
        if (normalizedUrl is not null)
        {
            upload = await dbContext.ImageUploads.SingleOrDefaultAsync(
                item =>
                    item.PublicUrl == normalizedUrl &&
                    item.OwnerId == ownerId &&
                    item.Kind == kind &&
                    item.Status == ImageUploadStatus.Pending,
                cancellationToken);
            if (upload is null)
                throw new ApiException(
                    StatusCodes.Status400BadRequest,
                    "Choose an image uploaded by this account before saving.");

            upload.Status = ImageUploadStatus.Claimed;
            upload.ClaimedAt = DateTimeOffset.UtcNow;
        }

        await MarkForDeletionAsync(currentObjectKey, cancellationToken);
        return new ClaimedImage(upload?.PublicUrl, upload?.ObjectKey);
    }

    public async Task MarkForDeletionAsync(
        string? objectKey,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(objectKey)) return;
        var upload = await dbContext.ImageUploads.SingleOrDefaultAsync(
            item => item.ObjectKey == objectKey,
            cancellationToken);
        if (upload is null || upload.Status is ImageUploadStatus.Deleted or ImageUploadStatus.Deleting)
            return;

        upload.Status = ImageUploadStatus.DeletePending;
        upload.AvailableAt = DateTimeOffset.UtcNow;
        upload.DeletionClaimedAt = null;
        upload.DeletionClaimedBy = null;
    }
}
