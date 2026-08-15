namespace EventManagement.Api.Models;

public sealed class BookingRequestStatusHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BookingRequestId { get; set; }
    public BookingRequestStatus Status { get; set; }
    public string? Note { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public BookingRequest? BookingRequest { get; set; }
}
