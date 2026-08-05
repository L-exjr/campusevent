namespace EventManagement.Api.Models;

public sealed class AdminAuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ActorUserId { get; set; }
    public required string Action { get; set; }
    public required string TargetType { get; set; }
    public required string TargetId { get; set; }
    public required string DetailsJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public User ActorUser { get; set; } = null!;
}
