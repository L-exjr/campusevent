using EventManagement.Api.DTOs.Audit;
using EventManagement.Api.DTOs.Common;
using EventManagement.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventManagement.Api.Controllers;

[ApiController]
[Route("api/admin-audit-logs")]
[Authorize(Roles = "Admin")]
public sealed class AdminAuditLogsController(AdminAuditService auditService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<AdminAuditLogResponse>>> Get(
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await auditService.GetAsync(search, page, pageSize, cancellationToken));

    [HttpGet("export")]
    public async Task<IActionResult> Export(
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        CancellationToken cancellationToken = default)
    {
        var content = await auditService.ExportCsvAsync(from, to, cancellationToken);
        return File(content, "text/csv; charset=utf-8", $"admin-audit-{DateTime.UtcNow:yyyyMMdd}.csv");
    }
}
