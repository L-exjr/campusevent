using EventManagement.Api.DTOs.Common;
using EventManagement.Api.DTOs.Events;
using EventManagement.Api.Infrastructure;
using EventManagement.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventManagement.Api.Controllers;

[ApiController]
[Route("api/events")]
public sealed class EventsController(IEventService eventService) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<EventResponse>>> Get(
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await eventService.GetAsync(
            search,
            category,
            from,
            to,
            page,
            pageSize,
            cancellationToken));

    [Authorize(Roles = "Organizer,Admin")]
    [HttpGet("mine")]
    public async Task<ActionResult<PaginatedResponse<EventResponse>>> GetMine(
        [FromQuery] bool upcoming = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await eventService.GetMineAsync(
            User.GetRequiredUserId(),
            upcoming,
            page,
            pageSize,
            cancellationToken));

    [Authorize(Roles = "Admin")]
    [HttpGet("all")]
    public async Task<ActionResult<PaginatedResponse<EventResponse>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await eventService.GetAllAsync(search, category, page, pageSize, cancellationToken));

    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EventResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken) =>
        Ok(await eventService.GetByIdAsync(id, cancellationToken));

    [Authorize(Roles = "Organizer,Admin")]
    [HttpGet("{id:guid}/management")]
    public async Task<ActionResult<EventResponse>> GetManagementById(
        Guid id,
        CancellationToken cancellationToken) =>
        Ok(await eventService.GetManagementByIdAsync(
            id,
            User.GetRequiredUserId(),
            User.GetRequiredRole(),
            cancellationToken));

    [Authorize(Roles = "Organizer,Admin")]
    [HttpPost]
    public async Task<ActionResult<EventResponse>> Create(
        EventUpsertRequest request,
        CancellationToken cancellationToken)
    {
        var eventResponse = await eventService.CreateAsync(
            User.GetRequiredUserId(),
            request,
            cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = eventResponse.Id }, eventResponse);
    }

    [Authorize(Roles = "Organizer,Admin")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<EventResponse>> Update(
        Guid id,
        EventUpsertRequest request,
        CancellationToken cancellationToken) =>
        Ok(await eventService.UpdateAsync(
            id,
            User.GetRequiredUserId(),
            User.GetRequiredRole(),
            request,
            cancellationToken));

    [Authorize(Roles = "Organizer,Admin")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await eventService.DeleteAsync(
            id,
            User.GetRequiredUserId(),
            User.GetRequiredRole(),
            cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = "Student")]
    [HttpPost("{id:guid}/register")]
    public async Task<ActionResult<StudentRegistrationResponse>> Register(
        Guid id,
        CancellationToken cancellationToken)
    {
        var registration = await eventService.RegisterAsync(
            id,
            User.GetRequiredUserId(),
            cancellationToken);
        return StatusCode(StatusCodes.Status201Created, registration);
    }

    [Authorize(Roles = "Student")]
    [HttpGet("{id:guid}/registration-status")]
    public async Task<ActionResult<RegistrationStatusResponse>> GetRegistrationStatus(
        Guid id,
        CancellationToken cancellationToken) =>
        Ok(new RegistrationStatusResponse(await eventService.IsRegisteredAsync(
            id,
            User.GetRequiredUserId(),
            cancellationToken)));

    [Authorize(Roles = "Organizer,Admin")]
    [HttpGet("{id:guid}/registrants")]
    public async Task<ActionResult<PaginatedResponse<EventRegistrantResponse>>> GetRegistrants(
        Guid id,
        [FromQuery] string? search = null,
        [FromQuery] bool? attended = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await eventService.GetRegistrantsAsync(
            id,
            User.GetRequiredUserId(),
            User.GetRequiredRole(),
            search,
            attended,
            page,
            pageSize,
            cancellationToken));

    [Authorize(Roles = "Organizer,Admin")]
    [HttpPut("{id:guid}/attendance")]
    public async Task<IActionResult> UpdateAttendance(
        Guid id,
        BulkAttendanceRequest request,
        CancellationToken cancellationToken)
    {
        await eventService.UpdateAttendanceAsync(
            id,
            User.GetRequiredUserId(),
            User.GetRequiredRole(),
            request,
            cancellationToken);
        return NoContent();
    }
}
