namespace EventManagement.Api.Models;

public sealed class BookingRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string OrganizationName { get; set; }
    public required string ContactName { get; set; }
    public required string Email { get; set; }
    public required string Phone { get; set; }
    public required string EventType { get; set; }
    public string? EventCategory { get; set; }
    public long? BudgetMinimumMinor { get; set; }
    public long? BudgetMaximumMinor { get; set; }
    public DateTimeOffset? ExpectedEndDate { get; set; }
    public bool RequiresTicketing { get; set; }
    public bool RequiresVoting { get; set; }
    public bool RequiresRegistration { get; set; }
    public string? ReferenceLinks { get; set; }
    public DateTimeOffset ProposedDate { get; set; }
    public string? AlternativeDates { get; set; }
    public string? FlexibilityNote { get; set; }
    public int EstimatedAttendance { get; set; }
    public string? PreferredOrganizer { get; set; }
    public Guid? RequestedOrganizerId { get; set; }
    public required string Description { get; set; }
    public BookingRequestStatus Status { get; set; } = BookingRequestStatus.Submitted;
    public Guid? AssignedOrganizerId { get; set; }
    public string? OrganizerResponseNote { get; set; }
    public Guid? DraftEventId { get; set; }
    public DateTimeOffset SubmittedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? PersonalDataAnonymizedAt { get; set; }
    public string? TrackingTokenHash { get; set; }

    public User? AssignedOrganizer { get; set; }
    public User? RequestedOrganizer { get; set; }
    public EventEntity? DraftEvent { get; set; }
    public BookingRequestQuote? Quote { get; set; }
    public ICollection<BookingRequestStatusHistory> StatusHistory { get; set; } = [];
}
