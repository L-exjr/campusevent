namespace EventManagement.Api.Models;

public sealed class BookingRequestQuote
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BookingRequestId { get; set; }
    public Guid OrganizerId { get; set; }
    public long ProposedFeeMinor { get; set; }
    public string Currency { get; set; } = "GHS";
    public required string ProposedTimeline { get; set; }
    public required string Message { get; set; }
    public DateTimeOffset SubmittedAt { get; set; } = DateTimeOffset.UtcNow;
    public BookingRequest? BookingRequest { get; set; }
    public User? Organizer { get; set; }
}
