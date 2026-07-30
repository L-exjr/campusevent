namespace EventManagement.Api.Models;

public enum BookingRequestStatus
{
    Submitted,
    UnderReview,
    SentToOrganizer,
    Accepted,
    Declined,
    Converted,
    Closed
}
