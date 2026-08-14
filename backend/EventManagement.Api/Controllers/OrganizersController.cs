using EventManagement.Api.DTOs.Common;
using EventManagement.Api.DTOs.Organizers;
using EventManagement.Api.Infrastructure;
using EventManagement.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventManagement.Api.Controllers;

[ApiController]
[Route("api/organizers")]
public sealed class OrganizersController(IOrganizerDirectoryService service) : ControllerBase
{
    [AllowAnonymous, HttpGet]
    public async Task<ActionResult<PaginatedResponse<PublicOrganizerSummary>>> Get([FromQuery] string? search, [FromQuery] string? category, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default) =>
        Ok(await service.GetPublicAsync(search, category, page, pageSize, cancellationToken));

    [AllowAnonymous, HttpGet("{id:guid}")]
    public async Task<ActionResult<PublicOrganizerDetail>> GetById(Guid id, CancellationToken cancellationToken) => Ok(await service.GetPublicByIdAsync(id, cancellationToken));

    [Authorize(Roles = "Student,Organizer"), HttpGet("me/settings")]
    public async Task<ActionResult<OrganizerDirectorySettings>> GetSettings(CancellationToken cancellationToken) => Ok(await service.GetSettingsAsync(User.GetRequiredUserId(), cancellationToken));

    [Authorize(Roles = "Student,Organizer"), HttpPut("me/settings")]
    public async Task<ActionResult<OrganizerDirectorySettings>> UpdateSettings(UpdateOrganizerDirectorySettings request, CancellationToken cancellationToken) => Ok(await service.UpdateSettingsAsync(User.GetRequiredUserId(), request, cancellationToken));
}
