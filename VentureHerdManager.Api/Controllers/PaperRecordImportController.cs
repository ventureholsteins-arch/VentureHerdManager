using Microsoft.AspNetCore.Mvc;
using VentureHerdManager.Api.Services;

namespace VentureHerdManager.Api.Controllers;

[ApiController]
[Route("api/paper-record-import")]
public sealed class PaperRecordImportController : ControllerBase
{
    private readonly PaperRecordImportService _service;
    private readonly IConfiguration _configuration;

    public PaperRecordImportController(
        PaperRecordImportService service,
        IConfiguration configuration)
    {
        _service = service;
        _configuration = configuration;
    }

    [HttpPost("preview")]
    public async Task<ActionResult<PaperImportReport>> Preview(
        CancellationToken cancellationToken) =>
        await _service.ReconcileAsync(null, false, cancellationToken);

    [HttpPost("apply")]
    public async Task<ActionResult<PaperImportReport>> Apply(
        CancellationToken cancellationToken)
    {
        if (!_configuration.GetValue<bool>("PaperRecordImport:AllowApply"))
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                "Paper import apply is disabled. Enable PaperRecordImport:AllowApply only for the controlled import window.");
        }

        return await _service.ReconcileAsync(null, true, cancellationToken);
    }
}
