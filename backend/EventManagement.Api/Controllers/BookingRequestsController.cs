using EventManagement.Api.DTOs.Bookings;
using EventManagement.Api.Infrastructure;
using EventManagement.Api.Models;
using EventManagement.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EventManagement.Api.Controllers;

[ApiController]
[Route("api/booking-requests")]
public sealed class BookingRequestsController(IBookingRequestService service) : ControllerBase
{
    [AllowAnonymous]
    [EnableRateLimiting("PublicBookingRequests")]
    [HttpPost]
    public async Task<ActionResult<BookingSubmissionResponse>> Submit(
        CreateBookingRequest request,
        CancellationToken cancellationToken) =>
        Accepted(await service.SubmitAsync(request, cancellationToken));

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BookingRequestResponse>>> GetAll(
        CancellationToken cancellationToken) => Ok(await service.GetAllAsync(cancellationToken));

    [Authorize(Roles = "Organizer")]
    [HttpGet("assigned")]
    public async Task<ActionResult<IReadOnlyList<BookingRequestResponse>>> GetAssigned(
        CancellationToken cancellationToken) =>
        Ok(await service.GetAssignedAsync(User.GetRequiredUserId(), cancellationToken));

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:guid}/assign")]
    public async Task<ActionResult<BookingRequestResponse>> Assign(
        Guid id, AssignBookingRequest request, CancellationToken cancellationToken) =>
        Ok(await service.AssignAsync(id, request.OrganizerId, cancellationToken));

    [Authorize(Roles = "Organizer")]
    [HttpPut("{id:guid}/respond")]
    public async Task<ActionResult<BookingRequestResponse>> Respond(
        Guid id, RespondToBookingRequest request, CancellationToken cancellationToken) =>
        Ok(await service.RespondAsync(id, User.GetRequiredUserId(), request, cancellationToken));

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:guid}/status")]
    public async Task<ActionResult<BookingRequestResponse>> UpdateStatus(
        Guid id, UpdateBookingRequestStatus request, CancellationToken cancellationToken) =>
        Ok(await service.UpdateStatusAsync(id, request.Status, cancellationToken));
}
