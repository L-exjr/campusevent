using EventManagement.Api.DTOs.Reports;
using EventManagement.Api.DTOs.Common;
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

    [HttpGet("events")]
    public async Task<ActionResult<PaginatedResponse<EventReportListItemResponse>>> GetEvents(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await reportService.GetEventsAsync(page, pageSize, cancellationToken));

    [HttpGet("organizers")]
    public async Task<ActionResult<IReadOnlyList<OrganizerReportResponse>>> GetOrganizers(
        CancellationToken cancellationToken) =>
        Ok(await reportService.GetOrganizersAsync(cancellationToken));
}
