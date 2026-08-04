using EventManagement.Api.DTOs.Bookings;
using EventManagement.Api.DTOs.Common;
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
    public async Task<ActionResult<PaginatedResponse<BookingRequestResponse>>> GetAll(
        [FromQuery] BookingRequestStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await service.GetAllAsync(status, page, pageSize, cancellationToken));

    [Authorize(Roles = "Organizer")]
    [HttpGet("assigned")]
    public async Task<ActionResult<PaginatedResponse<BookingRequestResponse>>> GetAssigned(
        [FromQuery] BookingRequestStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await service.GetAssignedAsync(
            User.GetRequiredUserId(), status, page, pageSize, cancellationToken));

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
