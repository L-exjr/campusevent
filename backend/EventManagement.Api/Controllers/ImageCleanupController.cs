using EventManagement.Api.DTOs.Common;
using EventManagement.Api.DTOs.Images;
using EventManagement.Api.Infrastructure;
using EventManagement.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventManagement.Api.Controllers;

[ApiController]
[Route("api/image-cleanup")]
[Authorize(Roles = "Admin")]
public sealed class ImageCleanupController(ImageCleanupAdministrationService service) : ControllerBase
{
    [HttpGet("failed")]
    public async Task<ActionResult<PaginatedResponse<FailedImageCleanupResponse>>> GetFailed(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await service.GetFailedAsync(page, pageSize, cancellationToken));

    [HttpPut("{id:guid}/retry")]
    public async Task<IActionResult> Retry(Guid id, CancellationToken cancellationToken)
    {
        await service.RetryAsync(id, User.GetRequiredUserId(), cancellationToken);
        return NoContent();
    }
}
