using EventManagement.Api.DTOs.Events;
using EventManagement.Api.Infrastructure;
using EventManagement.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventManagement.Api.Controllers;

[ApiController, Authorize(Roles = "Student,Organizer,Admin")]
[Route("api/events/{eventId:guid}")]
public sealed class EventTeamController(IEventTeamService teamService) : ControllerBase
{
    [HttpGet("access")]
    public async Task<ActionResult<EventAccessResponse>> GetAccess(Guid eventId, CancellationToken cancellationToken) =>
        Ok(await teamService.GetAccessAsync(eventId, User.GetRequiredUserId(), User.GetRequiredRole(), cancellationToken));

    [HttpGet("team")]
    public async Task<ActionResult<IReadOnlyList<EventTeamMemberResponse>>> Get(Guid eventId, CancellationToken cancellationToken) =>
        Ok(await teamService.GetAsync(eventId, User.GetRequiredUserId(), User.GetRequiredRole(), cancellationToken));

    [HttpGet("revenue")]
    public async Task<ActionResult<EventRevenueResponse>> GetRevenue(Guid eventId, CancellationToken cancellationToken) =>
        Ok(await teamService.GetRevenueAsync(eventId, User.GetRequiredUserId(), User.GetRequiredRole(), cancellationToken));

    [HttpPost("team")]
    public async Task<ActionResult<EventTeamMemberResponse>> Invite(Guid eventId, InviteEventTeamMemberRequest request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await teamService.InviteAsync(eventId, User.GetRequiredUserId(), User.GetRequiredRole(), request, cancellationToken));

    [HttpPut("team/{userId:guid}")]
    public async Task<ActionResult<EventTeamMemberResponse>> Update(Guid eventId, Guid userId, UpdateEventTeamMemberRequest request, CancellationToken cancellationToken) =>
        Ok(await teamService.UpdateAsync(eventId, userId, User.GetRequiredUserId(), User.GetRequiredRole(), request, cancellationToken));

    [HttpDelete("team/{userId:guid}")]
    public async Task<IActionResult> Remove(Guid eventId, Guid userId, CancellationToken cancellationToken)
    { await teamService.RemoveAsync(eventId, userId, User.GetRequiredUserId(), User.GetRequiredRole(), cancellationToken); return NoContent(); }
}
