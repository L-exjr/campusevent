using EventManagement.Api.DTOs.Events;
using EventManagement.Api.DTOs.Common;
using EventManagement.Api.Infrastructure;
using EventManagement.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventManagement.Api.Controllers;

[ApiController]
[Route("api/students")]
[Authorize(Roles = "Student,Organizer")]
public sealed class StudentsController(IEventService eventService) : ControllerBase
{
    [HttpGet("{id:guid}/registrations")]
    public async Task<ActionResult<PaginatedResponse<StudentRegistrationResponse>>> GetRegistrations(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (id != User.GetRequiredUserId())
            throw new ApiException(StatusCodes.Status403Forbidden, "Students may only view their own registrations.");
        return Ok(await eventService.GetStudentRegistrationsAsync(
            id, page, pageSize, cancellationToken));
    }
}
