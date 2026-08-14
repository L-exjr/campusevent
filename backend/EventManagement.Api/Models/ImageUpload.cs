namespace EventManagement.Api.Models;

public enum ImageUploadKind
{
    Profile,
    Event,
    OrganizerBanner
}

public enum ImageUploadStatus
{
    Pending,
    Claimed,
    DeletePending,
    Deleting,
    Deleted,
    Failed
}

public sealed class ImageUpload
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OwnerId { get; set; }
    public required string Bucket { get; set; }
    public required string ObjectKey { get; set; }
    public required string PublicUrl { get; set; }
    public ImageUploadKind Kind { get; set; }
    public ImageUploadStatus Status { get; set; } = ImageUploadStatus.Pending;
    public DateTimeOffset AvailableAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ClaimedAt { get; set; }
    public Guid? DeletionClaimedBy { get; set; }
    public DateTimeOffset? DeletionClaimedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public int DeleteAttemptCount { get; set; }
    public int LifetimeDeleteAttemptCount { get; set; }
    public int ManualRetryCount { get; set; }
    public DateTimeOffset? LastRetriedAt { get; set; }
    public string? LastError { get; set; }

    public User Owner { get; set; } = null!;
}
