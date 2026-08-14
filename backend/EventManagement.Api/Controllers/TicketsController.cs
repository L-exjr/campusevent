using EventManagement.Api.DTOs.Tickets;
using EventManagement.Api.Infrastructure;
using EventManagement.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventManagement.Api.Controllers;

[ApiController]
[Route("api/tickets")]
public sealed class TicketsController(ITicketService ticketService) : ControllerBase
{
    [Authorize(Roles = "Student,Organizer")]
    [HttpGet("{registrationId:guid}")]
    public async Task<ActionResult<TicketResponse>> Get(
        Guid registrationId,
        CancellationToken cancellationToken) =>
        Ok(await ticketService.GetAsync(
            registrationId,
            User.GetRequiredUserId(),
            cancellationToken));
}

[ApiController]
[Route("api/events/{eventId:guid}/check-in")]
public sealed class CheckInController(ITicketService ticketService) : ControllerBase
{
    [Authorize(Roles = "Student,Organizer,Admin")]
    [HttpPost]
    public async Task<ActionResult<CheckInResponse>> CheckIn(
        Guid eventId,
        CheckInRequest request,
        CancellationToken cancellationToken) =>
        Ok(await ticketService.CheckInAsync(
            eventId,
            User.GetRequiredUserId(),
            User.GetRequiredRole(),
            request.Token,
            cancellationToken));

    [Authorize(Roles = "Student,Organizer,Admin")]
    [HttpPost("manual")]
    public async Task<ActionResult<CheckInResponse>> CheckInManually(
        Guid eventId, ManualCheckInRequest request, CancellationToken cancellationToken) =>
        Ok(await ticketService.CheckInByCodeAsync(
            eventId, User.GetRequiredUserId(), User.GetRequiredRole(),
            request.TicketCode, cancellationToken));
}
