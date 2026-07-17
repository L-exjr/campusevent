using EventManagement.Api.DTOs.Events;
using EventManagement.Api.Infrastructure;
using EventManagement.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventManagement.Api.Controllers;

[ApiController]
[Route("api/students")]
[Authorize(Roles = "Student")]
public sealed class StudentsController(IEventService eventService) : ControllerBase
{
    [HttpGet("{id:guid}/registrations")]
    public async Task<ActionResult<IReadOnlyList<StudentRegistrationResponse>>> GetRegistrations(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (id != User.GetRequiredUserId())
            throw new ApiException(StatusCodes.Status403Forbidden, "Students may only view their own registrations.");
        return Ok(await eventService.GetStudentRegistrationsAsync(id, cancellationToken));
    }
}
