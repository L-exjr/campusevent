using EventManagement.Api.Data;
using EventManagement.Api.Infrastructure;
using EventManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Api.Services;

public enum EventCapability { ViewAttendees, CheckIn, Edit, ManageOperations, ViewRevenue, ManageTeam, Delete }

public interface IEventAuthorizationService
{
    Task EnsureCanAsync(Guid eventId, Guid eventOwnerId, Guid actorId, UserRole actorRole,
        EventCapability capability, CancellationToken cancellationToken);
}

public sealed class EventAuthorizationService(AppDbContext dbContext) : IEventAuthorizationService
{
    public async Task EnsureCanAsync(Guid eventId, Guid eventOwnerId, Guid actorId, UserRole actorRole,
        EventCapability capability, CancellationToken cancellationToken)
    {
        if (actorRole == UserRole.Admin || eventOwnerId == actorId) return;
        var teamRole = await dbContext.EventTeamMembers.AsNoTracking()
            .Where(member => member.EventId == eventId && member.UserId == actorId)
            .Select(member => (EventTeamRole?)member.Role).SingleOrDefaultAsync(cancellationToken);
        EnsureTeamCapability(teamRole, capability);
    }

    public static void EnsureTeamCapability(EventTeamRole? teamRole, EventCapability capability)
    {
        var allowed = teamRole switch
        {
            EventTeamRole.Admin => true,
            EventTeamRole.Member => capability is EventCapability.ViewAttendees or EventCapability.CheckIn
                or EventCapability.Edit or EventCapability.ManageOperations,
            EventTeamRole.CheckInStaff => capability is EventCapability.ViewAttendees or EventCapability.CheckIn,
            _ => false
        };
        if (!allowed) throw new ApiException(StatusCodes.Status403Forbidden,
            "You do not have permission to perform this action for this event.");
    }
}
