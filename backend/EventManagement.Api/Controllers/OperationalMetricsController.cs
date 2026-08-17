using EventManagement.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventManagement.Api.Controllers;

[ApiController]
[Route("api/operational-metrics")]
[Authorize(Roles = "Admin")]
public sealed class OperationalMetricsController(OperationalMetrics metrics) : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(metrics.Snapshot());
}
