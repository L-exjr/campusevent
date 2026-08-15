using System.ComponentModel.DataAnnotations;
using EventManagement.Api.Models;

namespace EventManagement.Api.DTOs.Bookings;

public sealed record CreateBookingRequest(
    [param: Required, StringLength(200, MinimumLength = 2)] string OrganizationName,
    [param: Required, StringLength(150, MinimumLength = 2)] string ContactName,
    [param: Required, EmailAddress, StringLength(320)] string Email,
    [param: Required, Phone, StringLength(50)] string Phone,
    [param: Required, StringLength(150, MinimumLength = 2)] string EventType,
    [param: StringLength(100)] string? EventCategory,
    [param: Range(0, long.MaxValue)] long? BudgetMinimumMinor,
    [param: Range(0, long.MaxValue)] long? BudgetMaximumMinor,
    DateTimeOffset ProposedDate,
    DateTimeOffset? ExpectedEndDate,
    [param: StringLength(500)] string? AlternativeDates,
    [param: StringLength(1000)] string? FlexibilityNote,
    [param: Range(1, 100000)] int EstimatedAttendance,
    bool RequiresTicketing,
    bool RequiresVoting,
    bool RequiresRegistration,
    [param: StringLength(4000)] string? ReferenceLinks,
    [param: StringLength(200)] string? PreferredOrganizer,
    Guid? RequestedOrganizerId,
    [param: Required, StringLength(5000, MinimumLength = 10)] string Description,
    [param: StringLength(200)] string? Website);

public sealed record AssignBookingRequest(Guid OrganizerId);

public sealed record RespondToBookingRequest(
    bool Accept,
    [param: StringLength(1000)] string? Note);

public sealed record SubmitBookingRequestQuote(
    [param: Range(0, long.MaxValue)] long ProposedFeeMinor,
    [param: Required, StringLength(500, MinimumLength = 2)] string ProposedTimeline,
    [param: Required, StringLength(1000, MinimumLength = 2)] string Message);

public sealed record UpdateBookingRequestStatus(BookingRequestStatus Status);

public sealed record BookingRequestResponse(
    Guid Id,
    string OrganizationName,
    string ContactName,
    string Email,
    string Phone,
    string EventType,
    string? EventCategory,
    long? BudgetMinimumMinor,
    long? BudgetMaximumMinor,
    DateTimeOffset ProposedDate,
    DateTimeOffset? ExpectedEndDate,
    string? AlternativeDates,
    string? FlexibilityNote,
    int EstimatedAttendance,
    bool RequiresTicketing,
    bool RequiresVoting,
    bool RequiresRegistration,
    string? ReferenceLinks,
    string? PreferredOrganizer,
    Guid? RequestedOrganizerId,
    string? RequestedOrganizerName,
    string Description,
    BookingRequestStatus Status,
    Guid? AssignedOrganizerId,
    string? AssignedOrganizerName,
    string? OrganizerResponseNote,
    Guid? DraftEventId,
    DateTimeOffset SubmittedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? PersonalDataAnonymizedAt,
    BookingRequestQuoteResponse? Quote,
    IReadOnlyList<BookingRequestStatusHistoryResponse> StatusHistory);

public sealed record BookingRequestQuoteResponse(
    Guid Id, long ProposedFeeMinor, string Currency, string ProposedTimeline,
    string Message, DateTimeOffset SubmittedAt);

public sealed record BookingRequestStatusHistoryResponse(
    Guid Id, BookingRequestStatus Status, string? Note, DateTimeOffset CreatedAt);

public sealed record TrackedBookingRequestResponse(
    Guid Id, string OrganizationName, string EventType, string? EventCategory,
    DateTimeOffset ProposedDate, DateTimeOffset? ExpectedEndDate, int EstimatedAttendance,
    BookingRequestStatus Status, BookingRequestQuoteResponse? Quote,
    IReadOnlyList<BookingRequestStatusHistoryResponse> StatusHistory, Guid? DraftEventId);

public sealed record BookingSubmissionResponse(string Message, Guid? Id, string? TrackingToken);
