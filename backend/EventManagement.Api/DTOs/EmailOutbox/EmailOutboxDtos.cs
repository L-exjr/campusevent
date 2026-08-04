namespace EventManagement.Api.DTOs.EmailOutbox;

public sealed record FailedEmailOutboxResponse(
    Guid Id,
    string Kind,
    Guid AggregateId,
    int AttemptCount,
    string? LastError,
    DateTimeOffset CreatedAt,
    bool CanRetry);
