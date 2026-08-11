using EventManagement.Api.DTOs.Certificates;
using EventManagement.Api.Infrastructure;
using EventManagement.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventManagement.Api.Controllers;

[ApiController]
[Authorize(Roles = "Student")]
[Route("api/certificates")]
public sealed class CertificatesController(ICertificateService certificateService) : ControllerBase
{
    [HttpPost("registrations/{registrationId:guid}")]
    public async Task<ActionResult<CertificateDownloadResponse>> GetOrCreate(
        Guid registrationId,
        CancellationToken cancellationToken) =>
        Ok(await certificateService.GetOrCreateAsync(
            registrationId,
            User.GetRequiredUserId(),
            cancellationToken));
}
