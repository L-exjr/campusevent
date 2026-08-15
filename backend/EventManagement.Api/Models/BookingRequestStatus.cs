namespace EventManagement.Api.Models;

public enum BookingRequestStatus
{
    Submitted,
    UnderReview,
    SentToOrganizer,
    Quoted,
    Accepted,
    Declined,
    Converted,
    Closed
}
