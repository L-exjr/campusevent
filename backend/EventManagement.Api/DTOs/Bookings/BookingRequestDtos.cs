using System.ComponentModel.DataAnnotations;
using EventManagement.Api.Models;

namespace EventManagement.Api.DTOs.Bookings;

public sealed record CreateBookingRequest(
    [param: Required, StringLength(200, MinimumLength = 2)] string OrganizationName,
    [param: Required, StringLength(150, MinimumLength = 2)] string ContactName,
    [param: Required, EmailAddress, StringLength(320)] string Email,
    [param: Required, Phone, StringLength(50)] string Phone,
    [param: Required, StringLength(150, MinimumLength = 2)] string EventType,
    DateTimeOffset ProposedDate,
    [param: StringLength(500)] string? AlternativeDates,
    [param: StringLength(1000)] string? FlexibilityNote,
    [param: Range(1, 100000)] int EstimatedAttendance,
    [param: StringLength(200)] string? PreferredOrganizer,
    Guid? RequestedOrganizerId,
    [param: Required, StringLength(5000, MinimumLength = 10)] string Description,
    [param: StringLength(200)] string? Website);

public sealed record AssignBookingRequest(Guid OrganizerId);

public sealed record RespondToBookingRequest(
    bool Accept,
    [param: StringLength(1000)] string? Note);

public sealed record UpdateBookingRequestStatus(BookingRequestStatus Status);

public sealed record BookingRequestResponse(
    Guid Id,
    string OrganizationName,
    string ContactName,
    string Email,
    string Phone,
    string EventType,
    DateTimeOffset ProposedDate,
    string? AlternativeDates,
    string? FlexibilityNote,
    int EstimatedAttendance,
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
    DateTimeOffset? PersonalDataAnonymizedAt);

public sealed record BookingSubmissionResponse(string Message, Guid? Id);
