namespace EventManagement.Api.Models;

public enum EmailOutboxStatus
{
    Pending,
    Processing,
    Sent,
    Failed,
    Discarded
}

public sealed class EmailOutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string IdempotencyKey { get; set; }
    public required string Kind { get; set; }
    public Guid AggregateId { get; set; }
    public string? PayloadJson { get; set; }
    public EmailOutboxStatus Status { get; set; } = EmailOutboxStatus.Pending;
    public DateTimeOffset AvailableAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ClaimedAt { get; set; }
    public Guid? ClaimedBy { get; set; }
    public int AttemptCount { get; set; }
    public int LifetimeAttemptCount { get; set; }
    public int ManualRetryCount { get; set; }
    public DateTimeOffset? LastRetriedAt { get; set; }
    public DateTimeOffset? SentAt { get; set; }
    public string? LastError { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
