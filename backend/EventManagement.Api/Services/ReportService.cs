using EventManagement.Api.Data;
using EventManagement.Api.DTOs.Reports;
using EventManagement.Api.DTOs.Common;
using EventManagement.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Api.Services;

public interface IReportService
{
    Task<ReportSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken);
    Task<EventReportResponse> GetEventAsync(Guid eventId, CancellationToken cancellationToken);
    Task<PaginatedResponse<EventReportListItemResponse>> GetEventsAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken);
    Task<PaginatedResponse<OrganizerReportResponse>> GetOrganizersAsync(
        int page, int pageSize, CancellationToken cancellationToken);
}

public sealed class ReportService(AppDbContext dbContext) : IReportService
{
    public async Task<ReportSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var totalEvents = await dbContext.Events.CountAsync(cancellationToken);
        var totalRegistrations = await dbContext.EventRegistrations.CountAsync(cancellationToken);
        var totalUsers = await dbContext.Users.CountAsync(cancellationToken);
        var attended = await dbContext.EventRegistrations.CountAsync(
            registration => registration.Attended,
            cancellationToken);
        return new ReportSummaryResponse(
            totalEvents,
            totalRegistrations,
            totalUsers,
            CalculateRate(attended, totalRegistrations));
    }

    public async Task<EventReportResponse> GetEventAsync(
        Guid eventId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Events.AsNoTracking()
            .Where(eventEntity => eventEntity.Id == eventId)
            .Select(eventEntity => new EventReportResponse(
                eventEntity.Id,
                eventEntity.Title,
                eventEntity.Registrations.Count,
                eventEntity.Registrations.Count(registration => registration.Attended),
                eventEntity.Registrations.Count == 0
                    ? 0
                    : Math.Round(
                        eventEntity.Registrations.Count(registration => registration.Attended) * 100m /
                        eventEntity.Registrations.Count,
                        2)))
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Event not found.");
    }

    public async Task<PaginatedResponse<EventReportListItemResponse>> GetEventsAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        (page, pageSize) = Pagination.Normalize(page, pageSize);
        var query = dbContext.Events.AsNoTracking();
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(eventEntity => eventEntity.Date)
            .ThenBy(eventEntity => eventEntity.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(eventEntity => new EventReportListItemResponse(
                eventEntity.Id,
                eventEntity.Title,
                eventEntity.OrganizerId,
                eventEntity.Organizer.Name,
                eventEntity.Registrations.Count,
                eventEntity.Registrations.Count(registration => registration.Attended),
                eventEntity.Registrations.Count == 0
                    ? 0
                    : Math.Round(
                        eventEntity.Registrations.Count(registration => registration.Attended) * 100m /
                        eventEntity.Registrations.Count,
                        2)))
            .ToListAsync(cancellationToken);
        return new PaginatedResponse<EventReportListItemResponse>(
            items,
            page,
            pageSize,
            totalCount,
            Pagination.TotalPages(totalCount, pageSize));
    }

    public async Task<PaginatedResponse<OrganizerReportResponse>> GetOrganizersAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        (page, pageSize) = Pagination.Normalize(page, pageSize);
        var query = dbContext.Users.AsNoTracking()
            .Where(user => dbContext.Events.Any(eventEntity =>
                eventEntity.OrganizerId == user.Id));
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Select(user => new
            {
                OrganizerId = user.Id,
                OrganizerName = user.Name,
                EventCount = dbContext.Events.Count(eventEntity =>
                    eventEntity.OrganizerId == user.Id),
                RegistrationCount = dbContext.EventRegistrations.Count(registration =>
                    registration.Event.OrganizerId == user.Id)
            })
            .OrderByDescending(organizer => organizer.RegistrationCount)
            .ThenByDescending(organizer => organizer.EventCount)
            .ThenBy(organizer => organizer.OrganizerId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(organizer => new OrganizerReportResponse(
                organizer.OrganizerId,
                organizer.OrganizerName,
                organizer.EventCount,
                organizer.RegistrationCount))
            .ToListAsync(cancellationToken);
        return new PaginatedResponse<OrganizerReportResponse>(
            items, page, pageSize, totalCount, Pagination.TotalPages(totalCount, pageSize));
    }

    private static decimal CalculateRate(int attended, int total) =>
        total == 0 ? 0 : Math.Round(attended * 100m / total, 2);
}
