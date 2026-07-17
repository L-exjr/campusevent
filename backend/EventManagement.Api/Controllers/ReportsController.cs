using EventManagement.Api.DTOs.Reports;
using EventManagement.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventManagement.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Roles = "Admin")]
public sealed class ReportsController(IReportService reportService) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult<ReportSummaryResponse>> GetSummary(
        CancellationToken cancellationToken) =>
        Ok(await reportService.GetSummaryAsync(cancellationToken));

    [HttpGet("events/{id:guid}")]
    public async Task<ActionResult<EventReportResponse>> GetEvent(
        Guid id,
        CancellationToken cancellationToken) =>
        Ok(await reportService.GetEventAsync(id, cancellationToken));

    [HttpGet("organizers")]
    public async Task<ActionResult<IReadOnlyList<OrganizerReportResponse>>> GetOrganizers(
        CancellationToken cancellationToken) =>
        Ok(await reportService.GetOrganizersAsync(cancellationToken));
}
