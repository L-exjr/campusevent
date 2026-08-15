using System.ComponentModel.DataAnnotations;
using EventManagement.Api.Models;

namespace EventManagement.Api.DTOs.Events;

public sealed record InviteEventTeamMemberRequest(
    [param: Required, EmailAddress, StringLength(320)] string Email,
    EventTeamRole Role);

public sealed record UpdateEventTeamMemberRequest(EventTeamRole Role);

public sealed record EventTeamMemberResponse(
    Guid UserId,
    string Name,
    string Email,
    EventTeamRole? Role,
    bool IsOwner,
    DateTimeOffset? JoinedAt);

public sealed record EventAccessResponse(
    bool CanViewAttendees,
    bool CanCheckIn,
    bool CanEdit,
    bool CanManageOperations,
    bool CanViewRevenue,
    bool CanManageTeam,
    bool CanDelete);

public sealed record EventRevenueResponse(long TicketRevenueMinor, string Currency);
