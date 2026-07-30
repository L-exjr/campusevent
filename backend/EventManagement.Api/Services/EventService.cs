using System.Data;
using EventManagement.Api.Data;
using EventManagement.Api.DTOs.Common;
using EventManagement.Api.DTOs.Events;
using EventManagement.Api.Infrastructure;
using EventManagement.Api.Mappings;
using EventManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

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
        int page,
        int pageSize,
        CancellationToken cancellationToken);
    Task<EventResponse> GetByIdAsync(Guid eventId, CancellationToken cancellationToken);
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
    Task DeleteAsync(
        Guid eventId,
        Guid actorId,
        UserRole actorRole,
        CancellationToken cancellationToken);
    Task<StudentRegistrationResponse> RegisterAsync(
        Guid eventId,
        Guid studentId,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<EventRegistrantResponse>> GetRegistrantsAsync(
        Guid eventId,
        Guid actorId,
        UserRole actorRole,
        CancellationToken cancellationToken);
    Task UpdateAttendanceAsync(
        Guid eventId,
        Guid actorId,
        UserRole actorRole,
        BulkAttendanceRequest request,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<StudentRegistrationResponse>> GetStudentRegistrationsAsync(
        Guid studentId,
        CancellationToken cancellationToken);
}

public sealed class EventService(
    AppDbContext dbContext,
    IEventAuthorizationService authorizationService,
    IEmailService emailService,
    ILogger<EventService> logger) : IEventService
{
    private static readonly string[] SupportedCategories =
        ["Academic", "Career", "Culture", "Sports", "Technology", "Wellness"];

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
        var query = dbContext.Events.AsNoTracking().AsQueryable();
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
        int page,
        int pageSize,
        CancellationToken cancellationToken) =>
        PaginateEventsAsync(
            dbContext.Events.AsNoTracking().Where(eventEntity => eventEntity.OrganizerId == userId),
            page,
            pageSize,
            cancellationToken);

    public async Task<EventResponse> GetByIdAsync(
        Guid eventId,
        CancellationToken cancellationToken)
    {
        return await ProjectEvents(
                dbContext.Events.AsNoTracking().Where(eventEntity => eventEntity.Id == eventId))
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Event not found.");
    }

    public async Task<EventResponse> CreateAsync(
        Guid actorId,
        EventUpsertRequest request,
        CancellationToken cancellationToken)
    {
        var input = NormalizeRequest(request);
        if (input.Date <= DateTimeOffset.UtcNow)
            throw new ApiException(StatusCodes.Status400BadRequest, "New events must be scheduled in the future.");
        var organizer = await dbContext.Users.SingleOrDefaultAsync(
            user => user.Id == actorId,
            cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Creating user not found.");
        if (organizer.Role is not (UserRole.Organizer or UserRole.Admin))
            throw new ApiException(StatusCodes.Status403Forbidden, "Only Organizers or Admins can create events.");
        var eventEntity = new EventEntity
        {
            Title = input.Title,
            Description = input.Description,
            Date = input.Date,
            Location = input.Location,
            Capacity = input.Capacity,
            Category = input.Category,
            ImageUrl = input.ImageUrl,
            OrganizerId = actorId,
            Organizer = organizer
        };
        dbContext.Events.Add(eventEntity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return eventEntity.ToResponse(0);
    }

    public async Task<EventResponse> UpdateAsync(
        Guid eventId,
        Guid actorId,
        UserRole actorRole,
        EventUpsertRequest request,
        CancellationToken cancellationToken)
    {
        var input = NormalizeRequest(request);
        var eventEntity = await dbContext.Events
            .Include(item => item.Organizer)
            .SingleOrDefaultAsync(item => item.Id == eventId, cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Event not found.");
        authorizationService.EnsureCanManage(eventEntity.OrganizerId, actorId, actorRole);
        var registrationCount = await dbContext.EventRegistrations.CountAsync(
            registration => registration.EventId == eventId,
            cancellationToken);
        if (input.Capacity < registrationCount)
            throw new ApiException(
                StatusCodes.Status409Conflict,
                $"Capacity cannot be lower than the current {registrationCount} registrations.");

        eventEntity.Title = input.Title;
        eventEntity.Description = input.Description;
        eventEntity.Date = input.Date;
        eventEntity.Location = input.Location;
        eventEntity.Capacity = input.Capacity;
        eventEntity.Category = input.Category;
        eventEntity.ImageUrl = input.ImageUrl;
        await dbContext.SaveChangesAsync(cancellationToken);
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
        authorizationService.EnsureCanManage(eventEntity.OrganizerId, actorId, actorRole);
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
        if (student.Role != UserRole.Student)
            throw new ApiException(StatusCodes.Status403Forbidden, "Only Students can register for events.");
        // Serialize registrations for this event. A unique student/event index prevents
        // duplicates, while this row lock makes the capacity check and insert atomic.
        var eventEntity = await dbContext.Events
            .FromSqlInterpolated(
                $"SELECT * FROM \"Events\" WHERE \"Id\" = {eventId} FOR UPDATE")
            .Include(item => item.Organizer)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Event not found.");
        if (eventEntity.Date <= DateTimeOffset.UtcNow)
            throw new ApiException(StatusCodes.Status409Conflict, "Registration has closed for this event.");
        if (await dbContext.EventRegistrations.AnyAsync(
            registration => registration.EventId == eventId && registration.StudentId == studentId,
            cancellationToken))
        {
            throw new ApiException(StatusCodes.Status409Conflict, "You are already registered for this event.");
        }
        var registrationCount = await dbContext.EventRegistrations.CountAsync(
            registration => registration.EventId == eventId,
            cancellationToken);
        if (registrationCount >= eventEntity.Capacity)
            throw new ApiException(StatusCodes.Status409Conflict, "This event is at capacity.");

        var registration = new EventRegistration
        {
            EventId = eventId,
            StudentId = studentId,
            Event = eventEntity,
            Student = student
        };
        dbContext.EventRegistrations.Add(registration);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw new ApiException(StatusCodes.Status409Conflict, "You are already registered for this event.");
        }

        try
        {
            await emailService.SendAsync(
                student.Email,
                student.Name,
                $"Registration confirmed: {eventEntity.Title}",
                "RegistrationConfirmation.html",
                new Dictionary<string, string?>
                {
                    ["StudentName"] = student.Name,
                    ["EventTitle"] = eventEntity.Title,
                    ["EventDate"] = eventEntity.Date.ToString("f"),
                    ["EventLocation"] = eventEntity.Location
                },
                cancellationToken);
        }
        catch (Exception exception)
        {
            // The registration is already committed. Email is best-effort and must
            // never turn a successful registration into an API failure.
            logger.LogError(
                exception,
                "Registration {RegistrationId} succeeded, but its confirmation email failed.",
                registration.Id);
        }

        return new StudentRegistrationResponse(
            registration.Id,
            registration.RegisteredAt,
            registration.Attended,
            eventEntity.ToResponse(registrationCount + 1));
    }

    public async Task<IReadOnlyList<EventRegistrantResponse>> GetRegistrantsAsync(
        Guid eventId,
        Guid actorId,
        UserRole actorRole,
        CancellationToken cancellationToken)
    {
        var eventEntity = await dbContext.Events.AsNoTracking().SingleOrDefaultAsync(
            item => item.Id == eventId,
            cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Event not found.");
        authorizationService.EnsureCanManage(eventEntity.OrganizerId, actorId, actorRole);
        return await dbContext.EventRegistrations.AsNoTracking()
            .Where(registration => registration.EventId == eventId)
            .OrderBy(registration => registration.Student.Name)
            .Select(registration => new EventRegistrantResponse(
                registration.Id,
                registration.StudentId,
                registration.Student.Name,
                registration.Student.Email,
                registration.RegisteredAt,
                registration.Attended))
            .ToListAsync(cancellationToken);
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
        authorizationService.EnsureCanManage(eventEntity.OrganizerId, actorId, actorRole);
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

    public async Task<IReadOnlyList<StudentRegistrationResponse>> GetStudentRegistrationsAsync(
        Guid studentId,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.Users.AsNoTracking().AnyAsync(
            user => user.Id == studentId,
            cancellationToken);
        if (!exists) throw new ApiException(StatusCodes.Status404NotFound, "Student account not found.");
        return await dbContext.EventRegistrations.AsNoTracking()
            .Where(registration => registration.StudentId == studentId)
            .OrderBy(registration => registration.Event.Date)
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
                    registration.Event.ImageUrl)))
            .ToListAsync(cancellationToken);
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
            eventEntity.ImageUrl));

    private static async Task<PaginatedResponse<EventResponse>> PaginateEventsAsync(
        IQueryable<EventEntity> query,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        (page, pageSize) = Pagination.Normalize(page, pageSize);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await ProjectEvents(query.OrderBy(eventEntity => eventEntity.Date))
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
        var location = request.Location.Trim();
        var requestedCategory = request.Category.Trim();
        var category = SupportedCategories.FirstOrDefault(item =>
            string.Equals(item, requestedCategory, StringComparison.OrdinalIgnoreCase));

        if (title.Length < 3)
            throw new ApiException(StatusCodes.Status400BadRequest, "Event titles must contain at least 3 characters.");
        if (description.Length < 10)
            throw new ApiException(StatusCodes.Status400BadRequest, "Event descriptions must contain at least 10 characters.");
        if (location.Length == 0)
            throw new ApiException(StatusCodes.Status400BadRequest, "An event location is required.");
        if (category is null)
            throw new ApiException(StatusCodes.Status400BadRequest, "Choose a supported event category.");

        return new NormalizedEventInput(
            title,
            description,
            request.Date,
            location,
            request.Capacity,
            category,
            string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim());
    }

    private readonly record struct NormalizedEventInput(
        string Title,
        string Description,
        DateTimeOffset Date,
        string Location,
        int Capacity,
        string Category,
        string? ImageUrl);
}
