using EventManagement.Api.DTOs.Applications;
using EventManagement.Api.DTOs.Common;
using EventManagement.Api.Infrastructure;
using EventManagement.Api.Models;
using EventManagement.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventManagement.Api.Controllers;

[ApiController]
[Route("api/organizer-applications")]
[Authorize]
public sealed class OrganizerApplicationsController(
    IOrganizerApplicationService applicationService) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "Student,Organizer")]
    public async Task<ActionResult<OrganizerApplicationResponse>> Submit(
        CreateOrganizerApplicationRequest request,
        CancellationToken cancellationToken)
    {
        var application = await applicationService.SubmitAsync(
            User.GetRequiredUserId(),
            request,
            cancellationToken);
        return StatusCode(StatusCodes.Status201Created, application);
    }

    [HttpGet("mine")]
    [Authorize(Roles = "Student,Organizer")]
    public async Task<ActionResult<OrganizerApplicationResponse?>> GetMine(
        CancellationToken cancellationToken) =>
        Ok(await applicationService.GetLatestForUserAsync(
            User.GetRequiredUserId(),
            cancellationToken));

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PaginatedResponse<OrganizerApplicationResponse>>> Get(
        [FromQuery] ApplicationStatus? status = ApplicationStatus.Pending,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await applicationService.GetAsync(status, search, page, pageSize, cancellationToken));

    [HttpPut("{id:guid}/approve")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<OrganizerApplicationResponse>> Approve(
        Guid id,
        CancellationToken cancellationToken) =>
        Ok(await applicationService.ApproveAsync(
            id,
            User.GetRequiredUserId(),
            cancellationToken));

    [HttpPut("{id:guid}/reject")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<OrganizerApplicationResponse>> Reject(
        Guid id,
        RejectOrganizerApplicationRequest request,
        CancellationToken cancellationToken) =>
        Ok(await applicationService.RejectAsync(
            id,
            User.GetRequiredUserId(),
            request,
            cancellationToken));
}
