namespace EventManagement.Api.Models;

public sealed class EventRegistration
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EventId { get; set; }
    public Guid StudentId { get; set; }
    public DateTimeOffset RegisteredAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ReminderSentAt { get; set; }
    public bool Attended { get; set; }

    public EventEntity Event { get; set; } = null!;
    public User Student { get; set; } = null!;
}
