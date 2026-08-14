using EventManagement.Api.Data;
using EventManagement.Api.Infrastructure;
using EventManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Api.Services;

public interface IEventImageAuthorizationService
{
    Task EnsureCanUploadAsync(Guid? eventId, Guid actorId, UserRole actorRole,
        CancellationToken cancellationToken);
}

public sealed class EventImageAuthorizationService(
    AppDbContext dbContext,
    IEventAuthorizationService eventAuthorizationService) : IEventImageAuthorizationService
{
    public async Task EnsureCanUploadAsync(
        Guid? eventId,
        Guid actorId,
        UserRole actorRole,
        CancellationToken cancellationToken)
    {
        if (actorRole == UserRole.Admin) return;
        if (!eventId.HasValue)
            throw new ApiException(StatusCodes.Status403Forbidden,
                "An event owner may only upload a cover for an existing event they own.");

        var eventOwnerId = await dbContext.Events.AsNoTracking()
            .Where(item => item.Id == eventId.Value)
            .Select(item => (Guid?)item.OrganizerId)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Event not found.");

        eventAuthorizationService.EnsureCanManage(eventOwnerId, actorId, actorRole);
    }
}
