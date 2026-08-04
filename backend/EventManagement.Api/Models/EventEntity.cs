namespace EventManagement.Api.Models;

public sealed class EventEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Title { get; set; }
    public required string Description { get; set; }
    public DateTimeOffset Date { get; set; }
    public required string Location { get; set; }
    public int Capacity { get; set; }
    public required string Category { get; set; }
    public string? ImageUrl { get; set; }
    public string? ImageObjectKey { get; set; }
    public Guid OrganizerId { get; set; }
    public bool IsPublished { get; set; } = true;
    public int Version { get; set; } = 1;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public User Organizer { get; set; } = null!;
    public ICollection<EventRegistration> Registrations { get; set; } = [];
    public BookingRequest? SourceBookingRequest { get; set; }
}
