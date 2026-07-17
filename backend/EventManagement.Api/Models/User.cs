namespace EventManagement.Api.Models;

public sealed class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public UserRole Role { get; set; } = UserRole.Student;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<EventEntity> OrganizedEvents { get; set; } = [];
    public ICollection<EventRegistration> Registrations { get; set; } = [];
    public ICollection<OrganizerApplication> OrganizerApplications { get; set; } = [];
}
