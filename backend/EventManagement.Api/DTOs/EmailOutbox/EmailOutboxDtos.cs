namespace EventManagement.Api.DTOs.EmailOutbox;

public sealed record FailedEmailOutboxResponse(
    Guid Id,
    string Kind,
    Guid AggregateId,
    int AttemptCount,
    int LifetimeAttemptCount,
    int ManualRetryCount,
    DateTimeOffset? LastRetriedAt,
    string? LastError,
    DateTimeOffset CreatedAt,
    bool CanRetry);
