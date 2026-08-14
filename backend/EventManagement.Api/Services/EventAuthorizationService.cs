using EventManagement.Api.Infrastructure;
using EventManagement.Api.Models;

namespace EventManagement.Api.Services;

public interface IEventAuthorizationService
{
    void EnsureCanManage(Guid eventOwnerId, Guid actorId, UserRole actorRole);
}

public sealed class EventAuthorizationService : IEventAuthorizationService
{
    public void EnsureCanManage(Guid eventOwnerId, Guid actorId, UserRole actorRole)
    {
        if (actorRole == UserRole.Admin) return;
        if (eventOwnerId != actorId)
        {
            throw new ApiException(
                StatusCodes.Status403Forbidden,
                "You may only manage your own events.");
        }
    }
}
