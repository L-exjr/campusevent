using EventManagement.Api.Infrastructure;
using EventManagement.Api.Models;

namespace EventManagement.Api.Services;

public static class StateTransitionRules
{
    private static readonly IReadOnlyDictionary<BookingRequestStatus, IReadOnlySet<BookingRequestStatus>>
        BookingTransitions = new Dictionary<BookingRequestStatus, IReadOnlySet<BookingRequestStatus>>
        {
            [BookingRequestStatus.Submitted] = new HashSet<BookingRequestStatus>
            {
                BookingRequestStatus.UnderReview,
                BookingRequestStatus.SentToOrganizer,
                BookingRequestStatus.Closed
            },
            [BookingRequestStatus.UnderReview] = new HashSet<BookingRequestStatus>
            {
                BookingRequestStatus.SentToOrganizer,
                BookingRequestStatus.Closed
            },
            [BookingRequestStatus.SentToOrganizer] = new HashSet<BookingRequestStatus>
            {
                BookingRequestStatus.Accepted,
                BookingRequestStatus.Declined,
                BookingRequestStatus.Closed
            },
            [BookingRequestStatus.Accepted] = new HashSet<BookingRequestStatus>
            {
                BookingRequestStatus.Converted,
                BookingRequestStatus.Closed
            },
            [BookingRequestStatus.Declined] = new HashSet<BookingRequestStatus>
            {
                BookingRequestStatus.Closed
            },
            [BookingRequestStatus.Converted] = new HashSet<BookingRequestStatus>
            {
                BookingRequestStatus.Closed
            },
            [BookingRequestStatus.Closed] = new HashSet<BookingRequestStatus>()
        };

    public static void EnsureBookingTransition(
        BookingRequestStatus current,
        BookingRequestStatus target)
    {
        if (BookingTransitions[current].Contains(target)) return;
        throw new ApiException(
            StatusCodes.Status409Conflict,
            $"A booking request cannot move from {current} to {target}.");
    }

    public static void EnsureEventPublicationTransition(
        bool? currentlyPublished,
        bool targetPublished,
        DateTimeOffset currentDate,
        DateTimeOffset targetDate,
        DateTimeOffset now)
    {
        if (!targetPublished) return;

        // Existing historical events remain editable when their date is unchanged,
        // but a draft cannot be published in the past and an event cannot be moved
        // into the past while remaining published.
        var isNewOrDraft = currentlyPublished != true;
        var dateChanged = targetDate != currentDate;
        if (targetDate <= now && (isNewOrDraft || dateChanged))
        {
            throw new ApiException(
                StatusCodes.Status400BadRequest,
                "Published events must be scheduled in the future.");
        }
    }
}
