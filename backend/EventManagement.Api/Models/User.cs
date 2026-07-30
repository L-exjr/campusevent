namespace EventManagement.Api.Models;

public sealed class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public required string Email { get; set; }
    public string? PasswordHash { get; set; }
    public AuthProvider AuthProvider { get; set; } = AuthProvider.Local;
    public string? GoogleSubject { get; set; }
    public UserRole Role { get; set; } = UserRole.Student;
    public bool IsActive { get; set; } = true;
    public string? ImageUrl { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<EventEntity> OrganizedEvents { get; set; } = [];
    public ICollection<EventRegistration> Registrations { get; set; } = [];
    public ICollection<OrganizerApplication> OrganizerApplications { get; set; } = [];
    public ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = [];
    public ICollection<BookingRequest> AssignedBookingRequests { get; set; } = [];
}
