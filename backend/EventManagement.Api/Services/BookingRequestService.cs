using EventManagement.Api.Data;
using EventManagement.Api.DTOs.Bookings;
using EventManagement.Api.Infrastructure;
using EventManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Api.Services;

public interface IBookingRequestService
{
    Task<BookingSubmissionResponse> SubmitAsync(CreateBookingRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<BookingRequestResponse>> GetAllAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<BookingRequestResponse>> GetAssignedAsync(Guid organizerId, CancellationToken cancellationToken);
    Task<BookingRequestResponse> AssignAsync(Guid id, Guid organizerId, CancellationToken cancellationToken);
    Task<BookingRequestResponse> RespondAsync(Guid id, Guid organizerId, RespondToBookingRequest request, CancellationToken cancellationToken);
    Task<BookingRequestResponse> UpdateStatusAsync(Guid id, BookingRequestStatus status, CancellationToken cancellationToken);
}

public sealed class BookingRequestService(AppDbContext dbContext) : IBookingRequestService
{
    private const string SubmissionMessage = "Your organizer request has been received.";

    public async Task<BookingSubmissionResponse> SubmitAsync(
        CreateBookingRequest request,
        CancellationToken cancellationToken)
    {
        // Bots commonly fill every input. Return the normal response so the trap
        // is not detectable, but never persist a honeypot submission.
        if (!string.IsNullOrWhiteSpace(request.Website))
            return new BookingSubmissionResponse(SubmissionMessage, null);
        if (request.ProposedDate <= DateTimeOffset.UtcNow)
            throw new ApiException(StatusCodes.Status400BadRequest, "The proposed date must be in the future.");

        var entity = new BookingRequest
        {
            OrganizationName = request.OrganizationName.Trim(),
            ContactName = request.ContactName.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            Phone = request.Phone.Trim(),
            EventType = request.EventType.Trim(),
            ProposedDate = request.ProposedDate,
            AlternativeDates = NormalizeOptional(request.AlternativeDates),
            FlexibilityNote = NormalizeOptional(request.FlexibilityNote),
            EstimatedAttendance = request.EstimatedAttendance,
            PreferredOrganizer = NormalizeOptional(request.PreferredOrganizer),
            Description = request.Description.Trim()
        };
        dbContext.BookingRequests.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new BookingSubmissionResponse(SubmissionMessage, entity.Id);
    }

    public async Task<IReadOnlyList<BookingRequestResponse>> GetAllAsync(CancellationToken cancellationToken) =>
        await Project(dbContext.BookingRequests.AsNoTracking())
            .OrderByDescending(request => request.SubmittedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<BookingRequestResponse>> GetAssignedAsync(
        Guid organizerId,
        CancellationToken cancellationToken) =>
        await Project(dbContext.BookingRequests.AsNoTracking()
                .Where(request => request.AssignedOrganizerId == organizerId))
            .OrderByDescending(request => request.SubmittedAt)
            .ToListAsync(cancellationToken);

    public async Task<BookingRequestResponse> AssignAsync(
        Guid id,
        Guid organizerId,
        CancellationToken cancellationToken)
    {
        var organizer = await dbContext.Users.SingleOrDefaultAsync(
            user => user.Id == organizerId && user.IsActive,
            cancellationToken);
        if (organizer is null || organizer.Role != UserRole.Organizer)
            throw new ApiException(StatusCodes.Status400BadRequest, "Choose an active Organizer.");
        var request = await FindAsync(id, cancellationToken);
        if (request.Status is BookingRequestStatus.Accepted or BookingRequestStatus.Declined or BookingRequestStatus.Converted or BookingRequestStatus.Closed)
            throw new ApiException(StatusCodes.Status409Conflict, "This request can no longer be assigned.");

        request.AssignedOrganizerId = organizerId;
        request.AssignedOrganizer = organizer;
        request.Status = BookingRequestStatus.SentToOrganizer;
        request.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(request);
    }

    public async Task<BookingRequestResponse> RespondAsync(
        Guid id,
        Guid organizerId,
        RespondToBookingRequest response,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var request = await dbContext.BookingRequests
            .Include(item => item.AssignedOrganizer)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Booking request not found.");
        if (request.AssignedOrganizerId != organizerId)
            throw new ApiException(StatusCodes.Status403Forbidden, "Only the assigned Organizer can respond.");
        if (request.Status != BookingRequestStatus.SentToOrganizer)
            throw new ApiException(StatusCodes.Status409Conflict, "This request is not awaiting an Organizer response.");

        request.OrganizerResponseNote = NormalizeOptional(response.Note);
        request.UpdatedAt = DateTimeOffset.UtcNow;
        if (!response.Accept)
        {
            request.Status = BookingRequestStatus.Declined;
        }
        else
        {
            request.Status = BookingRequestStatus.Accepted;
            var draft = new EventEntity
            {
                Title = $"{request.OrganizationName}: {request.EventType}"[..Math.Min(200, $"{request.OrganizationName}: {request.EventType}".Length)],
                Description = request.Description,
                Date = request.ProposedDate,
                Location = "To be confirmed",
                Capacity = request.EstimatedAttendance,
                Category = "Culture",
                OrganizerId = organizerId,
                Organizer = request.AssignedOrganizer!,
                IsPublished = false
            };
            dbContext.Events.Add(draft);
            request.DraftEvent = draft;
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ToResponse(request);
    }

    public async Task<BookingRequestResponse> UpdateStatusAsync(
        Guid id,
        BookingRequestStatus status,
        CancellationToken cancellationToken)
    {
        if (status is not (BookingRequestStatus.UnderReview or BookingRequestStatus.Converted or BookingRequestStatus.Closed))
            throw new ApiException(StatusCodes.Status400BadRequest, "Admin may only mark requests UnderReview, Converted, or Closed here.");
        var request = await FindAsync(id, cancellationToken);
        if (status == BookingRequestStatus.Converted && request.Status != BookingRequestStatus.Accepted)
            throw new ApiException(StatusCodes.Status409Conflict, "Only an accepted request can be converted.");
        request.Status = status;
        request.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(request);
    }

    private async Task<BookingRequest> FindAsync(Guid id, CancellationToken cancellationToken) =>
        await dbContext.BookingRequests.Include(request => request.AssignedOrganizer)
            .SingleOrDefaultAsync(request => request.Id == id, cancellationToken)
        ?? throw new ApiException(StatusCodes.Status404NotFound, "Booking request not found.");

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static IQueryable<BookingRequestResponse> Project(IQueryable<BookingRequest> query) =>
        query.Select(request => new BookingRequestResponse(
            request.Id, request.OrganizationName, request.ContactName, request.Email, request.Phone,
            request.EventType, request.ProposedDate, request.AlternativeDates, request.FlexibilityNote,
            request.EstimatedAttendance, request.PreferredOrganizer, request.Description, request.Status,
            request.AssignedOrganizerId, request.AssignedOrganizer == null ? null : request.AssignedOrganizer.Name,
            request.OrganizerResponseNote, request.DraftEventId, request.SubmittedAt, request.UpdatedAt));

    private static BookingRequestResponse ToResponse(BookingRequest request) => new(
        request.Id, request.OrganizationName, request.ContactName, request.Email, request.Phone,
        request.EventType, request.ProposedDate, request.AlternativeDates, request.FlexibilityNote,
        request.EstimatedAttendance, request.PreferredOrganizer, request.Description, request.Status,
        request.AssignedOrganizerId, request.AssignedOrganizer?.Name, request.OrganizerResponseNote,
        request.DraftEventId, request.SubmittedAt, request.UpdatedAt);
}
