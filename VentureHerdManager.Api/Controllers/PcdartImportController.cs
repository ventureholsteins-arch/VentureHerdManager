using Microsoft.AspNetCore.Mvc;
using VentureHerdManager.Api.DTOs;
using VentureHerdManager.Api.Services;

namespace VentureHerdManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class PcdartImportController : ControllerBase
{
    private readonly PcdartImportService _service;

    public PcdartImportController(PcdartImportService service)
    {
        _service = service;
    }

    [HttpPost("preview")]
    public async Task<ActionResult<PcdartImportResult>> Preview(
        [FromBody] PcdartImportRequest request,
        CancellationToken cancellationToken)
    {
        return await _service.ImportAsync(request, false, cancellationToken);
    }

    [HttpPost("apply")]
    public async Task<ActionResult<PcdartImportResult>> Apply(
        [FromBody] PcdartImportRequest request,
        CancellationToken cancellationToken)
    {
        return await _service.ImportAsync(request, true, cancellationToken);
    }
}