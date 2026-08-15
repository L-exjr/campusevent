using System.Data;
using System.Globalization;
using System.Text;
using EventManagement.Api.Data;
using EventManagement.Api.DTOs.Common;
using EventManagement.Api.DTOs.Events;
using EventManagement.Api.Infrastructure;
using EventManagement.Api.Mappings;
using EventManagement.Api.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EventManagement.Api.Services;

public interface IEventService
{
    Task<PaginatedResponse<EventResponse>> GetAsync(
        string? search,
        string? category,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
    Task<PaginatedResponse<EventResponse>> GetMineAsync(
        Guid userId,
        bool upcoming,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
    Task<PaginatedResponse<EventResponse>> GetAllAsync(
        string? search,
        string? category,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
    Task<EventResponse> GetByIdAsync(Guid eventId, CancellationToken cancellationToken);
    Task<EventResponse> GetManagementByIdAsync(
        Guid eventId,
        Guid actorId,
        UserRole actorRole,
        CancellationToken cancellationToken);
    Task<EventResponse> CreateAsync(
        Guid actorId,
        EventUpsertRequest request,
        CancellationToken cancellationToken);
    Task<EventResponse> UpdateAsync(
        Guid eventId,
        Guid actorId,
        UserRole actorRole,
        EventUpsertRequest request,
        CancellationToken cancellationToken);
    Task<EventResponse> TransferOwnershipAsync(
        Guid eventId,
        Guid adminId,
        TransferEventOwnershipRequest request,
        CancellationToken cancellationToken);
    Task DeleteAsync(
        Guid eventId,
        Guid actorId,
        UserRole actorRole,
        CancellationToken cancellationToken);
    Task<StudentRegistrationResponse> RegisterAsync(
        Guid eventId,
        Guid studentId,
        CancellationToken cancellationToken);
    Task<bool> IsRegisteredAsync(Guid eventId, Guid studentId, CancellationToken cancellationToken);
    Task<PaginatedResponse<EventRegistrantResponse>> GetRegistrantsAsync(
        Guid eventId,
        Guid actorId,
        UserRole actorRole,
        string? search,
        bool? attended,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
    Task UpdateAttendanceAsync(
        Guid eventId,
        Guid actorId,
        UserRole actorRole,
        BulkAttendanceRequest request,
        CancellationToken cancellationToken);
    Task<byte[]> ExportRegistrantsCsvAsync(
        Guid eventId, Guid actorId, UserRole actorRole, CancellationToken cancellationToken);
    Task<OrganizerAnalyticsResponse> GetOrganizerAnalyticsAsync(
        Guid organizerId, CancellationToken cancellationToken);
    Task<PaginatedResponse<StudentRegistrationResponse>> GetStudentRegistrationsAsync(
        Guid studentId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}

public sealed class EventService(
    AppDbContext dbContext,
    IEventAuthorizationService authorizationService,
    IImageLifecycleService imageLifecycleService,
    AdminAuditService auditService,
    TimeProvider timeProvider,
    IConfiguration configuration) : IEventService
{
    private static readonly string[] SupportedCategories =
    [
        "Art & Exhibition", "Awards Event", "Comedy Shows", "Concerts & Music",
        "Conferences", "Cultural Events", "Education & Learning", "Fashion & Beauty",
        "Festivals", "Food & Drink", "Gaming & Esports", "Hackathons",
        "Health & Wellness", "Movies & Film", "Other", "Pageant",
        "Parties & Nightlife", "Startup & Tech", "Workshops & Training"
    ];

    public async Task<PaginatedResponse<EventResponse>> GetAsync(
        string? search,
        string? category,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (from.HasValue && to.HasValue && from > to)
            throw new ApiException(StatusCodes.Status400BadRequest, "The from date must be before the to date.");
        var query = dbContext.Events.AsNoTracking().Where(eventEntity => eventEntity.IsPublished);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(eventEntity =>
                eventEntity.Title.ToLower().Contains(term) ||
                eventEntity.Description.ToLower().Contains(term) ||
                eventEntity.Location.ToLower().Contains(term));
        }
        if (!string.IsNullOrWhiteSpace(category))
        {
            var normalizedCategory = category.Trim().ToLowerInvariant();
            query = query.Where(eventEntity => eventEntity.Category.ToLower() == normalizedCategory);
        }
        if (from.HasValue) query = query.Where(eventEntity => eventEntity.Date >= from.Value);
        if (to.HasValue) query = query.Where(eventEntity => eventEntity.Date <= to.Value);
        return await PaginateEventsAsync(query, page, pageSize, cancellationToken);
    }

    public Task<PaginatedResponse<EventResponse>> GetMineAsync(
        Guid userId,
        bool upcoming,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Events.AsNoTracking()
            .Where(eventEntity => eventEntity.OrganizerId == userId ||
                eventEntity.TeamMembers.Any(member => member.UserId == userId));
        if (upcoming)
        {
            var now = timeProvider.GetUtcNow();
            query = query.Where(eventEntity => eventEntity.Date > now);
        }
        return PaginateEventsAsync(query, page, pageSize, cancellationToken);
    }

    public Task<PaginatedResponse<EventResponse>> GetAllAsync(
        string? search,
        string? category,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = ApplyEventFilters(dbContext.Events.AsNoTracking(), search, category);
        return PaginateEventsAsync(query, page, pageSize, cancellationToken);
    }

    public async Task<EventResponse> GetByIdAsync(
        Guid eventId,
        CancellationToken cancellationToken)
    {
        return await ProjectEvents(
                dbContext.Events.AsNoTracking().Where(eventEntity => eventEntity.Id == eventId && eventEntity.IsPublished))
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Event not found.");
    }

    public async Task<EventResponse> GetManagementByIdAsync(
        Guid eventId,
        Guid actorId,
        UserRole actorRole,
        CancellationToken cancellationToken)
    {
        var response = await ProjectEvents(
                dbContext.Events.AsNoTracking().Where(item => item.Id == eventId))
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Event not found.");
        await authorizationService.EnsureCanAsync(eventId, response.OrganizerId, actorId, actorRole,
            EventCapability.ViewAttendees, cancellationToken);
        return response;
    }

    public async Task<EventResponse> CreateAsync(
        Guid actorId,
        EventUpsertRequest request,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var input = NormalizeRequest(request);
        StateTransitionRules.EnsureEventPublicationTransition(
            null,
            request.IsPublished ?? true,
            default,
            input.Date,
            timeProvider.GetUtcNow());
        var organizer = await dbContext.Users
            .FromSqlInterpolated($"SELECT * FROM \"Users\" WHERE \"Id\" = {actorId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Creating user not found.");
        if (!organizer.IsActive)
            throw new ApiException(StatusCodes.Status403Forbidden, "An active account is required to create events.");
        if (input.PriceMinor > 0 && !configuration.GetValue("Payments:OrganizerSubaccountsEnabled", false))
            throw new ApiException(
                StatusCodes.Status409Conflict,
                "Paid event creation is unavailable until organizer Paystack settlement accounts are configured.");
        var image = await imageLifecycleService.ClaimAsync(
            actorId,
            ImageUploadKind.Event,
            input.ImageUrl,
            null,
            null,
            cancellationToken);
        var eventEntity = new EventEntity
        {
            Title = input.Title,
            Description = input.Description,
            Date = input.Date,
            EndDate = input.EndDate,
            Location = input.Location,
            Format = input.Format,
            MeetingUrl = input.MeetingUrl,
            VirtualPlatform = input.VirtualPlatform,
            Latitude = input.Latitude,
            Longitude = input.Longitude,
            InstagramUrl = input.InstagramUrl,
            TwitterUrl = input.TwitterUrl,
            FacebookUrl = input.FacebookUrl,
            WebsiteUrl = input.WebsiteUrl,
            TicketingEnabled = input.TicketingEnabled,
            RegistrationsEnabled = input.RegistrationsEnabled,
            VotingEnabled = input.VotingEnabled,
            SalesStartsAt = input.SalesStartsAt,
            SalesEndsAt = input.SalesEndsAt,
            Capacity = input.Capacity,
            PriceMinor = input.PriceMinor,
            Currency = input.Currency,
            Category = input.Category,
            ImageUrl = image.Url,
            ImageObjectKey = image.ObjectKey,
            OrganizerId = actorId,
            Organizer = organizer,
            IsPublished = request.IsPublished ?? true
        };
        var tierInputs = NormalizeTiers(request, input.PriceMinor, input.Capacity);
        foreach (var (tier, position) in tierInputs.Select((tier, position) => (tier, position)))
            eventEntity.TicketTiers.Add(new TicketTier
            {
                Name = tier.Name,
                PriceMinor = tier.PriceMinor,
                Capacity = tier.Capacity,
                Position = position
            });
        dbContext.Events.Add(eventEntity);
        if (organizer.Role == UserRole.Admin)
            auditService.Append(
                actorId,
                "EventCreated",
                "Event",
                eventEntity.Id,
                new { eventEntity.Title, eventEntity.IsPublished });
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return eventEntity.ToResponse(0);
    }

    public async Task<EventResponse> UpdateAsync(
        Guid eventId,
        Guid actorId,
        UserRole actorRole,
        EventUpsertRequest request,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var input = NormalizeRequest(request);
        var eventEntity = await dbContext.Events
            .Include(item => item.Organizer)
            .SingleOrDefaultAsync(item => item.Id == eventId, cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Event not found.");
        await authorizationService.EnsureCanAsync(eventId, eventEntity.OrganizerId, actorId, actorRole,
            EventCapability.Edit, cancellationToken);
        if (!request.Version.HasValue)
            throw new ApiException(StatusCodes.Status400BadRequest, "Refresh the event before saving changes.");
        if (request.Version.Value != eventEntity.Version)
            throw new ApiException(
                StatusCodes.Status409Conflict,
                "This event changed after you opened it. Refresh and try again.");
        var registrationCount = await dbContext.EventRegistrations.CountAsync(
            registration => registration.EventId == eventId,
            cancellationToken);
        if (request.TicketTiers is { Count: > 0 })
        {
            if (registrationCount > 0 || await dbContext.PaymentOrders.AnyAsync(
                order => order.EventId == eventId, cancellationToken))
                throw new ApiException(StatusCodes.Status409Conflict,
                    "Ticket tiers cannot change after registration or payment activity begins.");
            var replacementTiers = NormalizeTiers(request, input.PriceMinor, input.Capacity);
            await dbContext.Entry(eventEntity).Collection(item => item.TicketTiers)
                .LoadAsync(cancellationToken);
            dbContext.TicketTiers.RemoveRange(eventEntity.TicketTiers);
            foreach (var (tier, position) in replacementTiers.Select((tier, position) => (tier, position)))
                eventEntity.TicketTiers.Add(new TicketTier
                {
                    Name = tier.Name,
                    PriceMinor = tier.PriceMinor,
                    Capacity = tier.Capacity,
                    Position = position
                });
        }
        if (input.Capacity < registrationCount)
            throw new ApiException(
                StatusCodes.Status409Conflict,
                $"Capacity cannot be lower than the current {registrationCount} registrations.");

        if ((eventEntity.PriceMinor != input.PriceMinor || eventEntity.Currency != input.Currency) &&
            (registrationCount > 0 || await dbContext.PaymentOrders.AnyAsync(
                order => order.EventId == eventId,
                cancellationToken)))
        {
            throw new ApiException(
                StatusCodes.Status409Conflict,
                "Price and currency cannot change after registration or payment activity begins.");
        }

        var targetPublished = request.IsPublished ?? eventEntity.IsPublished;
        StateTransitionRules.EnsureEventPublicationTransition(
            eventEntity.IsPublished,
            targetPublished,
            eventEntity.Date,
            input.Date,
            timeProvider.GetUtcNow());

        if (eventEntity.PriceMinor <= 0 && input.PriceMinor > 0 &&
            !configuration.GetValue("Payments:OrganizerSubaccountsEnabled", false))
            throw new ApiException(
                StatusCodes.Status409Conflict,
                "Paid event creation is unavailable until organizer Paystack settlement accounts are configured.");

        eventEntity.Title = input.Title;
        eventEntity.Description = input.Description;
        eventEntity.Date = input.Date;
        eventEntity.EndDate = input.EndDate;
        eventEntity.Location = input.Location;
        eventEntity.Format = input.Format;
        eventEntity.MeetingUrl = input.MeetingUrl;
        eventEntity.VirtualPlatform = input.VirtualPlatform;
        eventEntity.Latitude = input.Latitude;
        eventEntity.Longitude = input.Longitude;
        eventEntity.InstagramUrl = input.InstagramUrl;
        eventEntity.TwitterUrl = input.TwitterUrl;
        eventEntity.FacebookUrl = input.FacebookUrl;
        eventEntity.WebsiteUrl = input.WebsiteUrl;
        eventEntity.TicketingEnabled = input.TicketingEnabled;
        eventEntity.RegistrationsEnabled = input.RegistrationsEnabled;
        eventEntity.VotingEnabled = input.VotingEnabled;
        eventEntity.SalesStartsAt = input.SalesStartsAt;
        eventEntity.SalesEndsAt = input.SalesEndsAt;
        eventEntity.Capacity = input.Capacity;
        eventEntity.PriceMinor = input.PriceMinor;
        eventEntity.Currency = input.Currency;
        eventEntity.Category = input.Category;
        var image = await imageLifecycleService.ClaimAsync(
            actorId,
            ImageUploadKind.Event,
            input.ImageUrl,
            eventEntity.ImageUrl,
            eventEntity.ImageObjectKey,
            cancellationToken);
        eventEntity.ImageUrl = image.Url;
        eventEntity.ImageObjectKey = image.ObjectKey;
        eventEntity.IsPublished = targetPublished;
        eventEntity.Version += 1;
        if (actorRole == UserRole.Admin)
            auditService.Append(
                actorId,
                "EventUpdated",
                "Event",
                eventEntity.Id,
                new { eventEntity.Title, eventEntity.IsPublished, eventEntity.Version });
        if (eventEntity.IsPublished)
        {
            var acceptedStatus = BookingRequestStatus.Accepted.ToString();
            var bookingRequest = await dbContext.BookingRequests
                .FromSqlInterpolated(
                    $"SELECT * FROM \"BookingRequests\" WHERE \"DraftEventId\" = {eventEntity.Id} AND \"Status\" = {acceptedStatus} FOR UPDATE")
                .SingleOrDefaultAsync(cancellationToken);
            if (bookingRequest is not null)
            {
                bookingRequest.Status = BookingRequestStatus.Converted;
                bookingRequest.UpdatedAt = timeProvider.GetUtcNow();
                dbContext.BookingRequestStatusHistory.Add(new BookingRequestStatusHistory
                {
                    BookingRequestId = bookingRequest.Id,
                    Status = BookingRequestStatus.Converted,
                    Note = "Private draft published and converted to an event.",
                    CreatedAt = bookingRequest.UpdatedAt
                });
            }
        }
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ApiException(
                StatusCodes.Status409Conflict,
                "This event changed after you opened it. Refresh and try again.");
        }
        await transaction.CommitAsync(cancellationToken);
        return eventEntity.ToResponse(registrationCount);
    }

    public async Task<EventResponse> TransferOwnershipAsync(
        Guid eventId,
        Guid adminId,
        TransferEventOwnershipRequest request,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var newOrganizer = await dbContext.Users
            .FromSqlInterpolated($"SELECT * FROM \"Users\" WHERE \"Id\" = {request.OrganizerId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Organizer not found.");
        if (!newOrganizer.IsActive || newOrganizer.Role == UserRole.Admin)
            throw new ApiException(
                StatusCodes.Status409Conflict,
                "Event ownership can only be transferred to an active ordinary user.");

        var eventEntity = await dbContext.Events
            .FromSqlInterpolated($"SELECT * FROM \"Events\" WHERE \"Id\" = {eventId} FOR UPDATE")
            .Include(item => item.Organizer)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Event not found.");
        if (eventEntity.Version != request.Version)
            throw new ApiException(
                StatusCodes.Status409Conflict,
                "This event changed after you opened it. Refresh and try again.");
        if (eventEntity.OrganizerId == newOrganizer.Id)
            throw new ApiException(StatusCodes.Status409Conflict, "This Organizer already owns the event.");

        var previousOrganizerId = eventEntity.OrganizerId;
        eventEntity.OrganizerId = newOrganizer.Id;
        eventEntity.Organizer = newOrganizer;
        eventEntity.Version += 1;

        var acceptedStatus = BookingRequestStatus.Accepted.ToString();
        var sourceRequest = await dbContext.BookingRequests
            .FromSqlInterpolated(
                $"SELECT * FROM \"BookingRequests\" WHERE \"DraftEventId\" = {eventId} AND \"Status\" = {acceptedStatus} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
        if (sourceRequest is not null)
        {
            sourceRequest.AssignedOrganizerId = newOrganizer.Id;
            sourceRequest.UpdatedAt = timeProvider.GetUtcNow();
        }

        var registrationCount = await dbContext.EventRegistrations.CountAsync(
            registration => registration.EventId == eventId,
            cancellationToken);
        auditService.Append(
            adminId,
            "EventOwnershipTransferred",
            "Event",
            eventEntity.Id,
            new { PreviousOrganizerId = previousOrganizerId, NewOrganizerId = newOrganizer.Id });
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ApiException(
                StatusCodes.Status409Conflict,
                "This event changed after you opened it. Refresh and try again.");
        }
        await transaction.CommitAsync(cancellationToken);
        return eventEntity.ToResponse(registrationCount);
    }

    public async Task DeleteAsync(
        Guid eventId,
        Guid actorId,
        UserRole actorRole,
        CancellationToken cancellationToken)
    {
        var eventEntity = await dbContext.Events.SingleOrDefaultAsync(
            item => item.Id == eventId,
            cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Event not found.");
        await authorizationService.EnsureCanAsync(eventId, eventEntity.OrganizerId, actorId, actorRole,
            EventCapability.Delete, cancellationToken);
        await imageLifecycleService.MarkForDeletionAsync(
            eventEntity.ImageObjectKey,
            cancellationToken);
        if (actorRole == UserRole.Admin)
            auditService.Append(
                actorId,
                "EventDeleted",
                "Event",
                eventEntity.Id,
                new { eventEntity.Title, eventEntity.OrganizerId });
        dbContext.Events.Remove(eventEntity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<StudentRegistrationResponse> RegisterAsync(
        Guid eventId,
        Guid studentId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var student = await dbContext.Users.SingleOrDefaultAsync(
            user => user.Id == studentId,
            cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Student account not found.");
        if (!student.IsActive)
            throw new ApiException(StatusCodes.Status403Forbidden, "An active account is required to register for events.");
        // Serialize registrations for this event. A unique student/event index prevents
        // duplicates, while this row lock makes the capacity check and insert atomic.
        var eventEntity = await dbContext.Events
            .FromSqlInterpolated(
                $"SELECT * FROM \"Events\" WHERE \"Id\" = {eventId} FOR UPDATE")
            .Include(item => item.Organizer)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Event not found.");
        if (eventEntity.Date <= timeProvider.GetUtcNow())
            throw new ApiException(StatusCodes.Status409Conflict, "Registration has closed for this event.");
        if (!eventEntity.IsPublished)
            throw new ApiException(StatusCodes.Status404NotFound, "Event not found.");
        if (!eventEntity.TicketingEnabled && !eventEntity.RegistrationsEnabled)
            throw new ApiException(StatusCodes.Status409Conflict, "This event is not accepting attendees.");
        if (eventEntity.PriceMinor > 0)
            throw new ApiException(
                StatusCodes.Status409Conflict,
                "This event requires verified payment before registration.");
        if (await dbContext.EventRegistrations.AnyAsync(
            registration => registration.EventId == eventId && registration.StudentId == studentId,
            cancellationToken))
        {
            throw new ApiException(StatusCodes.Status409Conflict, "You are already registered for this event.");
        }
        var registrationCount = await dbContext.EventRegistrations.CountAsync(
            registration => registration.EventId == eventId,
            cancellationToken);
        var now = timeProvider.GetUtcNow();
        var reservedPaymentCount = await dbContext.PaymentOrders.CountAsync(
            order => order.EventId == eventId &&
                order.Status == PaymentOrderStatus.Pending &&
                order.ExpiresAt > now,
            cancellationToken);
        if (registrationCount + reservedPaymentCount >= eventEntity.Capacity)
            throw new ApiException(StatusCodes.Status409Conflict, "This event is at capacity.");

        var registration = new EventRegistration
        {
            EventId = eventId,
            StudentId = studentId,
            Event = eventEntity,
            Student = student
        };
        dbContext.EventRegistrations.Add(registration);
        EmailOutbox.EnqueueDomainMessage(
            dbContext,
            $"registration-confirmation:{registration.Id}",
            EmailOutbox.RegistrationConfirmationKind,
            registration.Id);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: "IX_EventRegistrations_EventId_StudentId"
            })
        {
            throw new ApiException(StatusCodes.Status409Conflict, "You are already registered for this event.");
        }

        return new StudentRegistrationResponse(
            registration.Id,
            registration.RegisteredAt,
            registration.Attended,
            eventEntity.ToResponse(registrationCount + 1));
    }

    public Task<bool> IsRegisteredAsync(
        Guid eventId,
        Guid studentId,
        CancellationToken cancellationToken) =>
        dbContext.EventRegistrations.AsNoTracking().AnyAsync(
            registration => registration.EventId == eventId && registration.StudentId == studentId,
            cancellationToken);

    public async Task<PaginatedResponse<EventRegistrantResponse>> GetRegistrantsAsync(
        Guid eventId,
        Guid actorId,
        UserRole actorRole,
        string? search,
        bool? attended,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var eventEntity = await dbContext.Events.AsNoTracking().SingleOrDefaultAsync(
            item => item.Id == eventId,
            cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Event not found.");
        await authorizationService.EnsureCanAsync(eventId, eventEntity.OrganizerId, actorId, actorRole,
            EventCapability.ViewAttendees, cancellationToken);
        (page, pageSize) = Pagination.Normalize(page, pageSize);
        var query = dbContext.EventRegistrations.AsNoTracking()
            .Where(registration => registration.EventId == eventId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(registration =>
                registration.Student.Name.ToLower().Contains(term) ||
                registration.Student.Email.ToLower().Contains(term));
        }
        if (attended.HasValue)
            query = query.Where(registration => registration.Attended == attended.Value);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(registration => registration.Student.Name)
            .ThenBy(registration => registration.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(registration => new EventRegistrantResponse(
                registration.Id,
                registration.StudentId,
                registration.Student.Name,
                registration.Student.Email,
                registration.RegisteredAt,
                registration.Attended))
            .ToListAsync(cancellationToken);
        return new PaginatedResponse<EventRegistrantResponse>(
            items, page, pageSize, totalCount, Pagination.TotalPages(totalCount, pageSize));
    }

    public async Task UpdateAttendanceAsync(
        Guid eventId,
        Guid actorId,
        UserRole actorRole,
        BulkAttendanceRequest request,
        CancellationToken cancellationToken)
    {
        var eventEntity = await dbContext.Events.AsNoTracking().SingleOrDefaultAsync(
            item => item.Id == eventId,
            cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Event not found.");
        await authorizationService.EnsureCanAsync(eventId, eventEntity.OrganizerId, actorId, actorRole,
            EventCapability.CheckIn, cancellationToken);
        var ids = request.Registrations.Select(item => item.RegistrationId).ToList();
        if (ids.Count != ids.Distinct().Count())
            throw new ApiException(StatusCodes.Status400BadRequest, "Each registration may appear only once.");
        var registrations = await dbContext.EventRegistrations
            .Where(registration => registration.EventId == eventId && ids.Contains(registration.Id))
            .ToListAsync(cancellationToken);
        if (registrations.Count != ids.Count)
            throw new ApiException(
                StatusCodes.Status400BadRequest,
                "One or more registration IDs do not belong to this event.");

        var attendance = request.Registrations.ToDictionary(item => item.RegistrationId, item => item.Attended);
        foreach (var registration in registrations)
            registration.Attended = attendance[registration.Id];
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<byte[]> ExportRegistrantsCsvAsync(
        Guid eventId, Guid actorId, UserRole actorRole, CancellationToken cancellationToken)
    {
        var eventOwnerId = await dbContext.Events.AsNoTracking()
            .Where(item => item.Id == eventId)
            .Select(item => (Guid?)item.OrganizerId)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Event not found.");
        await authorizationService.EnsureCanAsync(eventId, eventOwnerId, actorId, actorRole,
            EventCapability.ViewAttendees, cancellationToken);
        var rows = await dbContext.EventRegistrations.AsNoTracking()
            .Where(item => item.EventId == eventId)
            .OrderBy(item => item.RegisteredAt)
            .Select(item => new { item.Student.Name, item.Student.Email, item.RegisteredAt, item.Attended })
            .ToListAsync(cancellationToken);
        var csv = new StringBuilder("Name,Email,Registration date,Checked in\r\n");
        foreach (var row in rows)
            csv.Append(Csv(row.Name)).Append(',').Append(Csv(row.Email)).Append(',')
                .Append(row.RegisteredAt.ToString("O", CultureInfo.InvariantCulture)).Append(',')
                .Append(row.Attended ? "Yes" : "No").Append("\r\n");
        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
    }

    public async Task<OrganizerAnalyticsResponse> GetOrganizerAnalyticsAsync(
        Guid organizerId, CancellationToken cancellationToken)
    {
        var registrations = await dbContext.EventRegistrations.AsNoTracking()
            .Where(item => item.Event.OrganizerId == organizerId)
            .Select(item => new { item.RegisteredAt, item.Attended })
            .ToListAsync(cancellationToken);
        var revenue = await dbContext.PaymentOrders.AsNoTracking()
            .Where(item => item.Event.OrganizerId == organizerId &&
                item.Status == PaymentOrderStatus.Verified)
            .SumAsync(item => (long?)item.AmountMinor, cancellationToken) ?? 0;
        var attended = registrations.Count(item => item.Attended);
        var points = registrations.GroupBy(item => DateOnly.FromDateTime(item.RegisteredAt.UtcDateTime))
            .OrderBy(group => group.Key)
            .Select(group => new OrganizerAnalyticsPoint(group.Key, group.Count())).ToList();
        return new OrganizerAnalyticsResponse(
            registrations.Count, revenue, "GHS", attended,
            registrations.Count == 0 ? 0 : Math.Round(attended * 100m / registrations.Count, 2), points);
    }

    private static string Csv(string value) =>
        $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";

    public async Task<PaginatedResponse<StudentRegistrationResponse>> GetStudentRegistrationsAsync(
        Guid studentId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.Users.AsNoTracking().AnyAsync(
            user => user.Id == studentId,
            cancellationToken);
        if (!exists) throw new ApiException(StatusCodes.Status404NotFound, "Student account not found.");
        (page, pageSize) = Pagination.Normalize(page, pageSize);
        var query = dbContext.EventRegistrations.AsNoTracking()
            .Where(registration => registration.StudentId == studentId);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(registration => registration.Event.Date)
            .ThenBy(registration => registration.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(registration => new StudentRegistrationResponse(
                registration.Id,
                registration.RegisteredAt,
                registration.Attended,
                new EventResponse(
                    registration.Event.Id,
                    registration.Event.Title,
                    registration.Event.Description,
                    registration.Event.Date,
                    registration.Event.Location,
                    registration.Event.Capacity,
                    registration.Event.Category,
                    registration.Event.OrganizerId,
                    registration.Event.Organizer.Name,
                    registration.Event.Registrations.Count,
                    registration.Event.CreatedAt,
                    registration.Event.ImageUrl,
                    registration.Event.IsPublished,
                    registration.Event.Version,
                    registration.Event.PriceMinor,
                    registration.Event.Currency,
                    registration.Event.Format,
                    registration.Event.MeetingUrl,
                    registration.Event.SalesStartsAt,
                    registration.Event.SalesEndsAt,
                    registration.Event.EndDate,
                    registration.Event.VirtualPlatform,
                    registration.Event.Latitude,
                    registration.Event.Longitude,
                    registration.Event.InstagramUrl,
                    registration.Event.TwitterUrl,
                    registration.Event.FacebookUrl,
                    registration.Event.WebsiteUrl,
                    registration.Event.TicketingEnabled,
                    registration.Event.RegistrationsEnabled,
                    registration.Event.VotingEnabled,
                    registration.Event.TicketTiers.OrderBy(tier => tier.Position)
                        .Select(tier => new TicketTierResponse(
                            tier.Id, tier.Name, tier.PriceMinor, tier.Capacity,
                            tier.PaymentOrders.Count(order => order.Status == PaymentOrderStatus.Verified),
                            tier.IsActive)).ToList())))
            .ToListAsync(cancellationToken);
        return new PaginatedResponse<StudentRegistrationResponse>(
            items, page, pageSize, totalCount, Pagination.TotalPages(totalCount, pageSize));
    }

    private static IQueryable<EventResponse> ProjectEvents(IQueryable<EventEntity> query) =>
        query.Select(eventEntity => new EventResponse(
            eventEntity.Id,
            eventEntity.Title,
            eventEntity.Description,
            eventEntity.Date,
            eventEntity.Location,
            eventEntity.Capacity,
            eventEntity.Category,
            eventEntity.OrganizerId,
            eventEntity.Organizer.Name,
            eventEntity.Registrations.Count,
            eventEntity.CreatedAt,
            eventEntity.ImageUrl,
            eventEntity.IsPublished,
            eventEntity.Version,
            eventEntity.PriceMinor,
            eventEntity.Currency,
            eventEntity.Format,
            eventEntity.MeetingUrl,
            eventEntity.SalesStartsAt,
            eventEntity.SalesEndsAt,
            eventEntity.EndDate,
            eventEntity.VirtualPlatform,
            eventEntity.Latitude,
            eventEntity.Longitude,
            eventEntity.InstagramUrl,
            eventEntity.TwitterUrl,
            eventEntity.FacebookUrl,
            eventEntity.WebsiteUrl,
            eventEntity.TicketingEnabled,
            eventEntity.RegistrationsEnabled,
            eventEntity.VotingEnabled,
            eventEntity.TicketTiers.OrderBy(tier => tier.Position)
                .Select(tier => new TicketTierResponse(
                    tier.Id, tier.Name, tier.PriceMinor, tier.Capacity,
                    tier.PaymentOrders.Count(order => order.Status == PaymentOrderStatus.Verified),
                    tier.IsActive)).ToList()));

    private static IQueryable<EventEntity> ApplyEventFilters(
        IQueryable<EventEntity> query,
        string? search,
        string? category)
    {
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(eventEntity =>
                eventEntity.Title.ToLower().Contains(term) ||
                eventEntity.Organizer.Name.ToLower().Contains(term));
        }
        if (!string.IsNullOrWhiteSpace(category))
        {
            var normalizedCategory = category.Trim().ToLowerInvariant();
            query = query.Where(eventEntity => eventEntity.Category.ToLower() == normalizedCategory);
        }
        return query;
    }

    private static async Task<PaginatedResponse<EventResponse>> PaginateEventsAsync(
        IQueryable<EventEntity> query,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        (page, pageSize) = Pagination.Normalize(page, pageSize);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await ProjectEvents(query
                .OrderBy(eventEntity => eventEntity.Date)
                .ThenBy(eventEntity => eventEntity.Id))
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return new PaginatedResponse<EventResponse>(
            items,
            page,
            pageSize,
            totalCount,
            Pagination.TotalPages(totalCount, pageSize));
    }

    private static NormalizedEventInput NormalizeRequest(EventUpsertRequest request)
    {
        var title = request.Title.Trim();
        var description = request.Description.Trim();
        var requestedFormat = request.Format.Trim();
        var format = string.Equals(requestedFormat, "Physical", StringComparison.OrdinalIgnoreCase)
            ? "Physical"
            : string.Equals(requestedFormat, "Virtual", StringComparison.OrdinalIgnoreCase)
                ? "Virtual"
                : string.Equals(requestedFormat, "Hybrid", StringComparison.OrdinalIgnoreCase)
                    ? "Hybrid"
                    : null;
        var location = request.Location.Trim();
        var meetingUrl = string.IsNullOrWhiteSpace(request.MeetingUrl) ? null : request.MeetingUrl.Trim();
        var requestedCategory = request.Category.Trim();
        var category = SupportedCategories.FirstOrDefault(item =>
            string.Equals(item, requestedCategory, StringComparison.OrdinalIgnoreCase));

        if (title.Length < 3)
            throw new ApiException(StatusCodes.Status400BadRequest, "Event titles must contain at least 3 characters.");
        if (description.Length < 10)
            throw new ApiException(StatusCodes.Status400BadRequest, "Event descriptions must contain at least 10 characters.");
        if (format is null)
            throw new ApiException(StatusCodes.Status400BadRequest, "Choose whether the event is physical, virtual, or hybrid.");
        if ((format == "Physical" || format == "Hybrid") && location.Length == 0)
            throw new ApiException(StatusCodes.Status400BadRequest, "An event location is required.");
        if ((format == "Virtual" || format == "Hybrid") &&
            (!Uri.TryCreate(meetingUrl, UriKind.Absolute, out var meetingUri) ||
             (meetingUri.Scheme != Uri.UriSchemeHttps && meetingUri.Scheme != Uri.UriSchemeHttp)))
            throw new ApiException(StatusCodes.Status400BadRequest, "A valid online meeting link is required.");
        if (category is null)
            throw new ApiException(StatusCodes.Status400BadRequest, "Choose a supported event category.");
        var endDate = request.EndDate ?? request.Date.AddHours(1);
        if (endDate <= request.Date)
            throw new ApiException(StatusCodes.Status400BadRequest, "Event end must be after the start.");
        if (request.TicketingEnabled && request.RegistrationsEnabled)
            throw new ApiException(StatusCodes.Status400BadRequest, "Ticketing and registrations cannot both be enabled.");

        if (request.PriceMinor < 0)
            throw new ApiException(StatusCodes.Status400BadRequest, "Event price cannot be negative.");
        if (request.PriceMinor > 0 && (request.SalesStartsAt is null || request.SalesEndsAt is null))
            throw new ApiException(StatusCodes.Status400BadRequest, "Paid events require a sales start and end time.");
        if (request.PriceMinor > 0 && request.SalesStartsAt >= request.SalesEndsAt)
            throw new ApiException(StatusCodes.Status400BadRequest, "Ticket sales must end after they start.");
        if (request.PriceMinor > 0 && request.SalesEndsAt > request.Date)
            throw new ApiException(StatusCodes.Status400BadRequest, "Ticket sales must end no later than the event start.");
        var currency = request.Currency.Trim().ToUpperInvariant();
        if (!string.Equals(currency, "GHS", StringComparison.Ordinal))
            throw new ApiException(StatusCodes.Status400BadRequest, "Only GHS pricing is currently supported.");

        return new NormalizedEventInput(
            title,
            description,
            request.Date,
            format == "Virtual" ? "Online" : location,
            request.Capacity,
            category,
            string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim(),
            request.PriceMinor,
            currency,
            format,
            format == "Physical" ? null : meetingUrl,
            request.PriceMinor > 0 ? request.SalesStartsAt : null,
            request.PriceMinor > 0 ? request.SalesEndsAt : null,
            endDate,
            format == "Physical" ? null : request.VirtualPlatform?.Trim(),
            format == "Virtual" ? null : request.Latitude,
            format == "Virtual" ? null : request.Longitude,
            request.InstagramUrl?.Trim(), request.TwitterUrl?.Trim(), request.FacebookUrl?.Trim(), request.WebsiteUrl?.Trim(),
            request.TicketingEnabled, request.RegistrationsEnabled, request.VotingEnabled);
    }

    private static IReadOnlyList<TicketTierInput> NormalizeTiers(
        EventUpsertRequest request, long fallbackPriceMinor, int fallbackCapacity)
    {
        var tiers = request.TicketTiers is { Count: > 0 }
            ? request.TicketTiers
            : [new TicketTierInput(null, "General", fallbackPriceMinor, fallbackCapacity)];
        if (tiers.Count > 20)
            throw new ApiException(StatusCodes.Status400BadRequest,
                "An event can have at most 20 ticket tiers.");
        var normalized = tiers.Select(tier => tier with { Name = tier.Name.Trim() }).ToList();
        if (normalized.Any(tier => string.IsNullOrWhiteSpace(tier.Name)) ||
            normalized.Select(tier => tier.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase).Count() != normalized.Count)
            throw new ApiException(StatusCodes.Status400BadRequest,
                "Ticket tier names must be unique and non-empty.");
        return normalized;
    }

    private readonly record struct NormalizedEventInput(
        string Title,
        string Description,
        DateTimeOffset Date,
        string Location,
        int Capacity,
        string Category,
        string? ImageUrl,
        long PriceMinor,
        string Currency,
        string Format,
        string? MeetingUrl,
        DateTimeOffset? SalesStartsAt,
        DateTimeOffset? SalesEndsAt,
        DateTimeOffset? EndDate,
        string? VirtualPlatform,
        double? Latitude,
        double? Longitude,
        string? InstagramUrl,
        string? TwitterUrl,
        string? FacebookUrl,
        string? WebsiteUrl,
        bool TicketingEnabled,
        bool RegistrationsEnabled,
        bool VotingEnabled);
}
