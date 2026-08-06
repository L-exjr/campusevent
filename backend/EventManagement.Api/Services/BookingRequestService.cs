using EventManagement.Api.Data;
using EventManagement.Api.DTOs.Bookings;
using EventManagement.Api.DTOs.Common;
using EventManagement.Api.Infrastructure;
using EventManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Api.Services;

public interface IBookingRequestService
{
    Task<BookingSubmissionResponse> SubmitAsync(CreateBookingRequest request, CancellationToken cancellationToken);
    Task<PaginatedResponse<BookingRequestResponse>> GetAllAsync(
        BookingRequestStatus? status, int page, int pageSize, CancellationToken cancellationToken);
    Task<PaginatedResponse<BookingRequestResponse>> GetAssignedAsync(
        Guid organizerId, BookingRequestStatus? status, int page, int pageSize,
        CancellationToken cancellationToken);
    Task<BookingRequestResponse> AssignAsync(
        Guid id, Guid organizerId, Guid adminId, CancellationToken cancellationToken);
    Task<BookingRequestResponse> RespondAsync(Guid id, Guid organizerId, RespondToBookingRequest request, CancellationToken cancellationToken);
    Task<BookingRequestResponse> UpdateStatusAsync(
        Guid id, BookingRequestStatus status, Guid adminId, CancellationToken cancellationToken);
}

public sealed class BookingRequestService(
    AppDbContext dbContext,
    AdminAuditService auditService,
    TimeProvider timeProvider) : IBookingRequestService
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
        if (request.ProposedDate <= timeProvider.GetUtcNow())
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

    public Task<PaginatedResponse<BookingRequestResponse>> GetAllAsync(
        BookingRequestStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken) =>
        PaginateAsync(dbContext.BookingRequests.AsNoTracking(), status, page, pageSize, cancellationToken);

    public Task<PaginatedResponse<BookingRequestResponse>> GetAssignedAsync(
        Guid organizerId,
        BookingRequestStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken) =>
        PaginateAsync(
            dbContext.BookingRequests.AsNoTracking()
                .Where(request => request.AssignedOrganizerId == organizerId),
            status,
            page,
            pageSize,
            cancellationToken);

    private static async Task<PaginatedResponse<BookingRequestResponse>> PaginateAsync(
        IQueryable<BookingRequest> query,
        BookingRequestStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        (page, pageSize) = Pagination.Normalize(page, pageSize);
        if (status.HasValue) query = query.Where(request => request.Status == status.Value);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await Project(query)
            .OrderByDescending(request => request.SubmittedAt)
            .ThenByDescending(request => request.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return new PaginatedResponse<BookingRequestResponse>(
            items, page, pageSize, totalCount, Pagination.TotalPages(totalCount, pageSize));
    }

    public async Task<BookingRequestResponse> AssignAsync(
        Guid id,
        Guid organizerId,
        Guid adminId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var organizer = await dbContext.Users
            .FromSqlInterpolated(
                $"SELECT * FROM \"Users\" WHERE \"Id\" = {organizerId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
        if (organizer is null || !organizer.IsActive || organizer.Role != UserRole.Organizer)
            throw new ApiException(StatusCodes.Status400BadRequest, "Choose an active Organizer.");
        var request = await FindForUpdateAsync(id, cancellationToken);
        if (request.Status != BookingRequestStatus.SentToOrganizer)
            StateTransitionRules.EnsureBookingTransition(
                request.Status,
                BookingRequestStatus.SentToOrganizer);

        var previousOrganizerId = request.AssignedOrganizerId;
        request.AssignedOrganizerId = organizerId;
        request.AssignedOrganizer = organizer;
        request.Status = BookingRequestStatus.SentToOrganizer;
        request.UpdatedAt = timeProvider.GetUtcNow();
        auditService.Append(
            adminId,
            previousOrganizerId.HasValue
                ? "BookingRequestReassigned"
                : "BookingRequestAssigned",
            "BookingRequest",
            request.Id,
            new { PreviousOrganizerId = previousOrganizerId, NewOrganizerId = organizerId });
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
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
            .FromSqlInterpolated(
                $"SELECT * FROM \"BookingRequests\" WHERE \"Id\" = {id} FOR UPDATE")
            .Include(item => item.AssignedOrganizer)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Booking request not found.");
        if (request.AssignedOrganizerId != organizerId)
            throw new ApiException(StatusCodes.Status403Forbidden, "Only the assigned Organizer can respond.");
        var responseStatus = response.Accept
            ? BookingRequestStatus.Accepted
            : BookingRequestStatus.Declined;
        StateTransitionRules.EnsureBookingTransition(request.Status, responseStatus);

        request.OrganizerResponseNote = NormalizeOptional(response.Note);
        request.UpdatedAt = timeProvider.GetUtcNow();
        if (!response.Accept)
        {
            request.Status = responseStatus;
        }
        else
        {
            request.Status = responseStatus;
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
        Guid adminId,
        CancellationToken cancellationToken)
    {
        if (status is not (BookingRequestStatus.UnderReview or BookingRequestStatus.Converted or BookingRequestStatus.Closed))
            throw new ApiException(StatusCodes.Status400BadRequest, "Admin may only mark requests UnderReview, Converted, or Closed here.");
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var request = await FindForUpdateAsync(id, cancellationToken);
        var previousStatus = request.Status;
        StateTransitionRules.EnsureBookingTransition(previousStatus, status);
        request.Status = status;
        request.UpdatedAt = timeProvider.GetUtcNow();
        auditService.Append(
            adminId,
            "BookingRequestStatusChanged",
            "BookingRequest",
            request.Id,
            new
            {
                PreviousStatus = previousStatus.ToString(),
                NewStatus = status.ToString()
            });
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ToResponse(request);
    }

    private async Task<BookingRequest> FindForUpdateAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        await dbContext.BookingRequests
            .FromSqlInterpolated(
                $"SELECT * FROM \"BookingRequests\" WHERE \"Id\" = {id} FOR UPDATE")
            .Include(request => request.AssignedOrganizer)
            .SingleOrDefaultAsync(cancellationToken)
        ?? throw new ApiException(StatusCodes.Status404NotFound, "Booking request not found.");

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static IQueryable<BookingRequestResponse> Project(IQueryable<BookingRequest> query) =>
        query.Select(request => new BookingRequestResponse(
            request.Id, request.OrganizationName, request.ContactName, request.Email, request.Phone,
            request.EventType, request.ProposedDate, request.AlternativeDates, request.FlexibilityNote,
            request.EstimatedAttendance, request.PreferredOrganizer, request.Description, request.Status,
            request.AssignedOrganizerId, request.AssignedOrganizer == null ? null : request.AssignedOrganizer.Name,
            request.OrganizerResponseNote, request.DraftEventId, request.SubmittedAt, request.UpdatedAt,
            request.PersonalDataAnonymizedAt));

    private static BookingRequestResponse ToResponse(BookingRequest request) => new(
        request.Id, request.OrganizationName, request.ContactName, request.Email, request.Phone,
        request.EventType, request.ProposedDate, request.AlternativeDates, request.FlexibilityNote,
        request.EstimatedAttendance, request.PreferredOrganizer, request.Description, request.Status,
        request.AssignedOrganizerId, request.AssignedOrganizer?.Name, request.OrganizerResponseNote,
        request.DraftEventId, request.SubmittedAt, request.UpdatedAt,
        request.PersonalDataAnonymizedAt);
}
