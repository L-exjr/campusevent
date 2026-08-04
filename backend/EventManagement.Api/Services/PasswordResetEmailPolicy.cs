namespace EventManagement.Api.Services;

public static class PasswordResetEmailPolicy
{
    public static bool ShouldDeliver(
        bool userIsActive,
        DateTimeOffset? usedAt,
        DateTimeOffset expiresAt,
        DateTimeOffset now) =>
        userIsActive && !usedAt.HasValue && expiresAt > now;
}
