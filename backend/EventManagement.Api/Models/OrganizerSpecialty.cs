namespace EventManagement.Api.Models;

public sealed class OrganizerSpecialty
{
    public Guid OrganizerId { get; set; }
    public required string Category { get; set; }
    public User Organizer { get; set; } = null!;
}
