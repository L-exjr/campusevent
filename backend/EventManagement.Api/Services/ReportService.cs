using EventManagement.Api.Data;
using EventManagement.Api.DTOs.Reports;
using EventManagement.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Api.Services;

public interface IReportService
{
    Task<ReportSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken);
    Task<EventReportResponse> GetEventAsync(Guid eventId, CancellationToken cancellationToken);
    Task<IReadOnlyList<OrganizerReportResponse>> GetOrganizersAsync(CancellationToken cancellationToken);
}

public sealed class ReportService(AppDbContext dbContext) : IReportService
{
    public async Task<ReportSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var totalEvents = await dbContext.Events.CountAsync(cancellationToken);
        var totalRegistrations = await dbContext.EventRegistrations.CountAsync(cancellationToken);
        var attended = await dbContext.EventRegistrations.CountAsync(
            registration => registration.Attended,
            cancellationToken);
        return new ReportSummaryResponse(
            totalEvents,
            totalRegistrations,
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

    public async Task<IReadOnlyList<OrganizerReportResponse>> GetOrganizersAsync(
        CancellationToken cancellationToken)
    {
        return await dbContext.Users.AsNoTracking()
            .Where(user => dbContext.Events.Any(eventEntity =>
                eventEntity.OrganizerId == user.Id))
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
            .Select(organizer => new OrganizerReportResponse(
                organizer.OrganizerId,
                organizer.OrganizerName,
                organizer.EventCount,
                organizer.RegistrationCount))
            .ToListAsync(cancellationToken);
    }

    private static decimal CalculateRate(int attended, int total) =>
        total == 0 ? 0 : Math.Round(attended * 100m / total, 2);
}
