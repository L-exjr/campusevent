namespace EventManagement.Api.Models;

public sealed class AuthRateLimitBucket
{
    public required string Key { get; set; }
    public DateTimeOffset WindowStartedAt { get; set; }
    public int AttemptCount { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
