using EventManagement.Api.Data;
using EventManagement.Api.DTOs.Events;
using EventManagement.Api.Infrastructure;
using EventManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Api.Services;

public interface IEventTeamService
{
    Task<IReadOnlyList<EventTeamMemberResponse>> GetAsync(Guid eventId, Guid actorId, UserRole actorRole, CancellationToken cancellationToken);
    Task<EventAccessResponse> GetAccessAsync(Guid eventId, Guid actorId, UserRole actorRole, CancellationToken cancellationToken);
    Task<EventRevenueResponse> GetRevenueAsync(Guid eventId, Guid actorId, UserRole actorRole, CancellationToken cancellationToken);
    Task<EventTeamMemberResponse> InviteAsync(Guid eventId, Guid actorId, UserRole actorRole, InviteEventTeamMemberRequest request, CancellationToken cancellationToken);
    Task<EventTeamMemberResponse> UpdateAsync(Guid eventId, Guid userId, Guid actorId, UserRole actorRole, UpdateEventTeamMemberRequest request, CancellationToken cancellationToken);
    Task RemoveAsync(Guid eventId, Guid userId, Guid actorId, UserRole actorRole, CancellationToken cancellationToken);
}

public sealed class EventTeamService(AppDbContext dbContext, IEventAuthorizationService authorizationService, TimeProvider timeProvider) : IEventTeamService
{
    public async Task<IReadOnlyList<EventTeamMemberResponse>> GetAsync(Guid eventId, Guid actorId, UserRole actorRole, CancellationToken cancellationToken)
    {
        var eventEntity = await GetEventAsync(eventId, cancellationToken);
        await authorizationService.EnsureCanAsync(eventId, eventEntity.OrganizerId, actorId, actorRole, EventCapability.ManageTeam, cancellationToken);
        var members = await dbContext.EventTeamMembers.AsNoTracking().Where(item => item.EventId == eventId)
            .OrderBy(item => item.User.Name)
            .Select(item => new EventTeamMemberResponse(item.UserId, item.User.Name, item.User.Email, item.Role, false, item.CreatedAt))
            .ToListAsync(cancellationToken);
        members.Insert(0, new EventTeamMemberResponse(eventEntity.OrganizerId, eventEntity.Organizer.Name,
            eventEntity.Organizer.Email, null, true, null));
        return members;
    }

    public async Task<EventAccessResponse> GetAccessAsync(Guid eventId, Guid actorId, UserRole actorRole, CancellationToken cancellationToken)
    {
        var ownerId = await dbContext.Events.AsNoTracking().Where(item => item.Id == eventId)
            .Select(item => (Guid?)item.OrganizerId).SingleOrDefaultAsync(cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Event not found.");
        if (actorRole == UserRole.Admin || ownerId == actorId) return AllAccess();
        var role = await dbContext.EventTeamMembers.AsNoTracking().Where(item => item.EventId == eventId && item.UserId == actorId)
            .Select(item => (EventTeamRole?)item.Role).SingleOrDefaultAsync(cancellationToken);
        return role switch
        {
            EventTeamRole.Admin => AllAccess(),
            EventTeamRole.Member => new(true, true, true, true, false, false, false),
            EventTeamRole.CheckInStaff => new(true, true, false, false, false, false, false),
            _ => throw new ApiException(StatusCodes.Status403Forbidden, "You are not part of this event team.")
        };
    }

    public async Task<EventRevenueResponse> GetRevenueAsync(Guid eventId, Guid actorId, UserRole actorRole, CancellationToken cancellationToken)
    {
        var eventEntity = await GetEventAsync(eventId, cancellationToken);
        await authorizationService.EnsureCanAsync(eventId, eventEntity.OrganizerId, actorId, actorRole, EventCapability.ViewRevenue, cancellationToken);
        var revenue = await dbContext.PaymentOrders.AsNoTracking()
            .Where(item => item.EventId == eventId && item.Status == PaymentOrderStatus.Verified)
            .SumAsync(item => item.AmountMinor, cancellationToken);
        return new(revenue, eventEntity.Currency);
    }

    public async Task<EventTeamMemberResponse> InviteAsync(Guid eventId, Guid actorId, UserRole actorRole, InviteEventTeamMemberRequest request, CancellationToken cancellationToken)
    {
        var eventEntity = await GetEventAsync(eventId, cancellationToken);
        await authorizationService.EnsureCanAsync(eventId, eventEntity.OrganizerId, actorId, actorRole, EventCapability.ManageTeam, cancellationToken);
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await dbContext.Users.SingleOrDefaultAsync(item => item.Email.ToLower() == email, cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "No account exists for that email address. Ask them to create an account first.");
        if (!user.IsActive) throw new ApiException(StatusCodes.Status409Conflict, "A deactivated account cannot join an event team.");
        if (user.Id == eventEntity.OrganizerId) throw new ApiException(StatusCodes.Status409Conflict, "The event owner already has full access.");
        if (user.Role == UserRole.Admin) throw new ApiException(StatusCodes.Status409Conflict, "Platform Admins already have event access and cannot be team members.");
        if (await dbContext.EventTeamMembers.AnyAsync(item => item.EventId == eventId && item.UserId == user.Id, cancellationToken))
            throw new ApiException(StatusCodes.Status409Conflict, "This user is already on the event team.");
        var now = timeProvider.GetUtcNow();
        dbContext.EventTeamMembers.Add(new EventTeamMember { EventId = eventId, UserId = user.Id, Role = request.Role, InvitedByUserId = actorId, CreatedAt = now, UpdatedAt = now });
        await dbContext.SaveChangesAsync(cancellationToken);
        return new(user.Id, user.Name, user.Email, request.Role, false, now);
    }

    public async Task<EventTeamMemberResponse> UpdateAsync(Guid eventId, Guid userId, Guid actorId, UserRole actorRole, UpdateEventTeamMemberRequest request, CancellationToken cancellationToken)
    {
        var eventEntity = await GetEventAsync(eventId, cancellationToken);
        await authorizationService.EnsureCanAsync(eventId, eventEntity.OrganizerId, actorId, actorRole, EventCapability.ManageTeam, cancellationToken);
        if (userId == eventEntity.OrganizerId) throw new ApiException(StatusCodes.Status409Conflict, "The event owner's role cannot be changed.");
        var member = await dbContext.EventTeamMembers.Include(item => item.User)
            .SingleOrDefaultAsync(item => item.EventId == eventId && item.UserId == userId, cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Event team member not found.");
        member.Role = request.Role; member.UpdatedAt = timeProvider.GetUtcNow();
        await dbContext.SaveChangesAsync(cancellationToken);
        return new(member.UserId, member.User.Name, member.User.Email, member.Role, false, member.CreatedAt);
    }

    public async Task RemoveAsync(Guid eventId, Guid userId, Guid actorId, UserRole actorRole, CancellationToken cancellationToken)
    {
        var eventEntity = await GetEventAsync(eventId, cancellationToken);
        await authorizationService.EnsureCanAsync(eventId, eventEntity.OrganizerId, actorId, actorRole, EventCapability.ManageTeam, cancellationToken);
        if (userId == eventEntity.OrganizerId) throw new ApiException(StatusCodes.Status409Conflict, "The event owner cannot be removed.");
        var member = await dbContext.EventTeamMembers.SingleOrDefaultAsync(item => item.EventId == eventId && item.UserId == userId, cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Event team member not found.");
        dbContext.EventTeamMembers.Remove(member); await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<EventEntity> GetEventAsync(Guid eventId, CancellationToken cancellationToken) =>
        await dbContext.Events.AsNoTracking().Include(item => item.Organizer).SingleOrDefaultAsync(item => item.Id == eventId, cancellationToken)
        ?? throw new ApiException(StatusCodes.Status404NotFound, "Event not found.");
    private static EventAccessResponse AllAccess() => new(true, true, true, true, true, true, true);
}
