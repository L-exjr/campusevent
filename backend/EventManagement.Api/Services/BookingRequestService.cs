using EventManagement.Api.Data;
using EventManagement.Api.DTOs.Bookings;
using EventManagement.Api.DTOs.Common;
using EventManagement.Api.Infrastructure;
using EventManagement.Api.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

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
    Task<BookingRequestResponse> SubmitQuoteAsync(Guid id, Guid organizerId, SubmitBookingRequestQuote request, CancellationToken cancellationToken);
    Task<TrackedBookingRequestResponse> TrackAsync(Guid id, string token, CancellationToken cancellationToken);
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
            return new BookingSubmissionResponse(SubmissionMessage, null, null);
        if (request.ProposedDate <= timeProvider.GetUtcNow())
            throw new ApiException(StatusCodes.Status400BadRequest, "The proposed date must be in the future.");
        if (request.ExpectedEndDate.HasValue && request.ExpectedEndDate < request.ProposedDate)
            throw new ApiException(StatusCodes.Status400BadRequest, "The expected end date cannot be before the start date.");
        if (request.BudgetMinimumMinor.HasValue && request.BudgetMaximumMinor.HasValue && request.BudgetMinimumMinor > request.BudgetMaximumMinor)
            throw new ApiException(StatusCodes.Status400BadRequest, "The minimum budget cannot exceed the maximum budget.");
        User? requestedOrganizer = null;
        if (request.RequestedOrganizerId.HasValue)
        {
            requestedOrganizer = await dbContext.Users.AsNoTracking().SingleOrDefaultAsync(user =>
                user.Id == request.RequestedOrganizerId && user.IsActive &&
                user.Role != UserRole.Admin && user.IsOrganizerDirectoryVisible && user.OrganizedEvents.Any(),
                cancellationToken);
            if (requestedOrganizer is null)
                throw new ApiException(StatusCodes.Status400BadRequest, "The selected Organizer is no longer available in the public directory.");
        }

        var trackingToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var entity = new BookingRequest
        {
            OrganizationName = request.OrganizationName.Trim(),
            ContactName = request.ContactName.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            Phone = request.Phone.Trim(),
            EventType = request.EventType.Trim(),
            EventCategory = NormalizeOptional(request.EventCategory),
            BudgetMinimumMinor = request.BudgetMinimumMinor,
            BudgetMaximumMinor = request.BudgetMaximumMinor,
            ProposedDate = request.ProposedDate,
            ExpectedEndDate = request.ExpectedEndDate,
            AlternativeDates = NormalizeOptional(request.AlternativeDates),
            FlexibilityNote = NormalizeOptional(request.FlexibilityNote),
            EstimatedAttendance = request.EstimatedAttendance,
            RequiresTicketing = request.RequiresTicketing,
            RequiresVoting = request.RequiresVoting,
            RequiresRegistration = request.RequiresRegistration,
            ReferenceLinks = NormalizeOptional(request.ReferenceLinks),
            PreferredOrganizer = NormalizeOptional(request.PreferredOrganizer),
            RequestedOrganizerId = requestedOrganizer?.Id,
            Description = request.Description.Trim(),
            TrackingTokenHash = HashToken(trackingToken)
        };
        entity.StatusHistory.Add(new BookingRequestStatusHistory
        {
            BookingRequestId = entity.Id,
            Status = BookingRequestStatus.Submitted,
            Note = "Request submitted.",
            CreatedAt = entity.SubmittedAt
        });
        dbContext.BookingRequests.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new BookingSubmissionResponse(SubmissionMessage, entity.Id, trackingToken);
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
        var pageQuery = query
            .OrderByDescending(request => request.SubmittedAt)
            .ThenByDescending(request => request.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize);
        var entities = await pageQuery
            .Include(request => request.AssignedOrganizer)
            .Include(request => request.RequestedOrganizer)
            .Include(request => request.Quote)
            .Include(request => request.StatusHistory)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);
        var items = entities.Select(ToResponse).ToList();
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
        if (organizer is null || !organizer.IsActive || organizer.Role == UserRole.Admin)
            throw new ApiException(StatusCodes.Status400BadRequest, "Choose an active ordinary user.");
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
        AppendHistory(request, BookingRequestStatus.SentToOrganizer, $"Assigned to {organizer.Name}.", request.UpdatedAt);
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
            .Include(item => item.RequestedOrganizer)
            .Include(item => item.Quote)
            .Include(item => item.StatusHistory)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Booking request not found.");
        if (request.AssignedOrganizerId != organizerId)
            throw new ApiException(StatusCodes.Status403Forbidden, "Only the assigned Organizer can respond.");
        var responseStatus = response.Accept
            ? BookingRequestStatus.Accepted
            : BookingRequestStatus.Declined;
        StateTransitionRules.EnsureBookingTransition(request.Status, responseStatus);
        if (response.Accept && request.Quote is null)
            throw new ApiException(StatusCodes.Status409Conflict, "Submit a quote before accepting this request.");

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
                Category = "Cultural Events",
                EndDate = request.ProposedDate.AddHours(2),
                RegistrationsEnabled = true,
                OrganizerId = organizerId,
                Organizer = request.AssignedOrganizer!,
                IsPublished = false,
                TicketTiers =
                [
                    new TicketTier
                    {
                        Name = "General",
                        PriceMinor = 0,
                        Capacity = request.EstimatedAttendance,
                        Position = 0
                    }
                ]
            };
            dbContext.Events.Add(draft);
            request.DraftEvent = draft;
        }
        AppendHistory(request, responseStatus,
            response.Accept ? "Quote accepted and private event draft created." : "Organizer declined the request.",
            request.UpdatedAt);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ToResponse(request);
    }

    public async Task<BookingRequestResponse> SubmitQuoteAsync(
        Guid id, Guid organizerId, SubmitBookingRequestQuote quote, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var request = await FindForUpdateAsync(id, cancellationToken);
        if (request.AssignedOrganizerId != organizerId)
            throw new ApiException(StatusCodes.Status403Forbidden, "Only the assigned Organizer can quote.");
        StateTransitionRules.EnsureBookingTransition(request.Status, BookingRequestStatus.Quoted);
        var now = timeProvider.GetUtcNow();
        request.Quote = new BookingRequestQuote
        {
            BookingRequestId = request.Id,
            OrganizerId = organizerId,
            ProposedFeeMinor = quote.ProposedFeeMinor,
            ProposedTimeline = quote.ProposedTimeline.Trim(),
            Message = quote.Message.Trim(),
            SubmittedAt = now
        };
        dbContext.BookingRequestQuotes.Add(request.Quote);
        request.Status = BookingRequestStatus.Quoted;
        request.UpdatedAt = now;
        AppendHistory(request, BookingRequestStatus.Quoted, "Organizer submitted a quote.", now);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ToResponse(request);
    }

    public async Task<TrackedBookingRequestResponse> TrackAsync(
        Guid id, string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ApiException(StatusCodes.Status404NotFound, "Booking request not found.");
        var hash = HashToken(token);
        var request = await dbContext.BookingRequests.AsNoTracking()
            .Include(item => item.Quote)
            .Include(item => item.StatusHistory)
            .AsSplitQuery()
            .SingleOrDefaultAsync(item => item.Id == id && item.TrackingTokenHash == hash, cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Booking request not found.");
        return new TrackedBookingRequestResponse(
            request.Id, request.OrganizationName, request.EventType, request.EventCategory,
            request.ProposedDate, request.ExpectedEndDate, request.EstimatedAttendance, request.Status,
            ToQuoteResponse(request.Quote), ToHistoryResponse(request.StatusHistory), request.DraftEventId);
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
        AppendHistory(request, status, $"Status changed to {status}.", request.UpdatedAt);
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
            .Include(request => request.RequestedOrganizer)
            .Include(request => request.Quote)
            .Include(request => request.StatusHistory)
            .SingleOrDefaultAsync(cancellationToken)
        ?? throw new ApiException(StatusCodes.Status404NotFound, "Booking request not found.");

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static BookingRequestResponse ToResponse(BookingRequest request) => new(
        request.Id, request.OrganizationName, request.ContactName, request.Email, request.Phone,
        request.EventType, request.EventCategory, request.BudgetMinimumMinor, request.BudgetMaximumMinor,
        request.ProposedDate, request.ExpectedEndDate, request.AlternativeDates, request.FlexibilityNote,
        request.EstimatedAttendance, request.RequiresTicketing, request.RequiresVoting,
        request.RequiresRegistration, request.ReferenceLinks, request.PreferredOrganizer, request.RequestedOrganizerId,
        request.RequestedOrganizer?.Name, request.Description, request.Status,
        request.AssignedOrganizerId, request.AssignedOrganizer?.Name, request.OrganizerResponseNote,
        request.DraftEventId, request.SubmittedAt, request.UpdatedAt,
        request.PersonalDataAnonymizedAt, ToQuoteResponse(request.Quote), ToHistoryResponse(request.StatusHistory));

    private static BookingRequestQuoteResponse? ToQuoteResponse(BookingRequestQuote? quote) => quote is null ? null :
        new(quote.Id, quote.ProposedFeeMinor, quote.Currency, quote.ProposedTimeline, quote.Message, quote.SubmittedAt);

    private static IReadOnlyList<BookingRequestStatusHistoryResponse> ToHistoryResponse(
        IEnumerable<BookingRequestStatusHistory> history) => history.OrderBy(item => item.CreatedAt)
        .Select(item => new BookingRequestStatusHistoryResponse(item.Id, item.Status, item.Note, item.CreatedAt)).ToList();

    private void AppendHistory(BookingRequest request, BookingRequestStatus status, string note, DateTimeOffset at)
    {
        var history = new BookingRequestStatusHistory
        {
            BookingRequestId = request.Id, Status = status, Note = note, CreatedAt = at
        };
        request.StatusHistory.Add(history);
        dbContext.BookingRequestStatusHistory.Add(history);
    }

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
}
