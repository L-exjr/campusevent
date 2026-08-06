namespace EventManagement.Api.DTOs.Images;

public sealed record FailedImageCleanupResponse(
    Guid Id,
    string Bucket,
    string ObjectKey,
    string Kind,
    int DeleteAttemptCount,
    int LifetimeDeleteAttemptCount,
    int ManualRetryCount,
    DateTimeOffset? LastRetriedAt,
    string? LastError,
    DateTimeOffset CreatedAt);
