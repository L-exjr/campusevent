namespace EventManagement.Api.Services;

public interface IImageStorageService
{
    Task<StoredImage> UploadImageAsync(
        Stream content,
        string contentType,
        string bucket,
        string extension,
        Guid ownerId,
        CancellationToken cancellationToken = default);

    Task DeleteImageAsync(
        string bucket,
        string objectKey,
        CancellationToken cancellationToken = default);
}

public sealed record StoredImage(string ObjectKey, string PublicUrl);
