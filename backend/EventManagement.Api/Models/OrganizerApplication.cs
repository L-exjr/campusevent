namespace EventManagement.Api.Models;

public sealed class OrganizerApplication
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public required string Reason { get; set; }
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;
    public string? RejectionReason { get; set; }
    public DateTimeOffset SubmittedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ReviewedAt { get; set; }
    public Guid? ReviewedByAdminId { get; set; }

    public User User { get; set; } = null!;
    public User? ReviewedByAdmin { get; set; }
}
