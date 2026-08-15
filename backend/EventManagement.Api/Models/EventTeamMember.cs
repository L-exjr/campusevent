namespace EventManagement.Api.Models;

public enum EventTeamRole { Admin, Member, CheckInStaff }

public sealed class EventTeamMember
{
    public Guid EventId { get; set; }
    public Guid UserId { get; set; }
    public EventTeamRole Role { get; set; }
    public Guid InvitedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public EventEntity Event { get; set; } = null!;
    public User User { get; set; } = null!;
    public User InvitedByUser { get; set; } = null!;
}
