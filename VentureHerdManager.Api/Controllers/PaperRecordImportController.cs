using Microsoft.AspNetCore.Mvc;
using VentureHerdManager.Api.Services;

namespace VentureHerdManager.Api.Controllers;

[ApiController]
[Route("api/paper-record-import")]
public sealed class PaperRecordImportController : ControllerBase
{
    private readonly PaperRecordImportService _service;
    private readonly IConfiguration _configuration;
    private readonly HerdDataAdminAccess _admin;
    private readonly ILogger<PaperRecordImportController> _logger;

    public PaperRecordImportController(
        PaperRecordImportService service,
        IConfiguration configuration,
        HerdDataAdminAccess admin,
        ILogger<PaperRecordImportController> logger)
    {
        _service = service;
        _configuration = configuration;
        _admin = admin;
        _logger = logger;
    }

    [HttpPost("preview")]
    public async Task<ActionResult<PaperImportReport>> Preview(
        CancellationToken cancellationToken)
    {
        if (!_admin.IsAuthorized(Request)) return Unauthorized();
        return await _service.ReconcileAsync(null, false, cancellationToken);
    }

    [HttpPost("apply")]
    public async Task<ActionResult<PaperImportReport>> Apply(
        CancellationToken cancellationToken)
    {
        if (!_admin.IsAuthorized(Request)) return Unauthorized();
        if (!_configuration.GetValue<bool>("PaperRecordImport:AllowApply"))
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                "Paper import apply is disabled. Enable PaperRecordImport:AllowApply only for the controlled import window.");
        }

        try
        {
            return await _service.ReconcileAsync(null, true, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Controlled paper-record reconciliation failed.");
            return Problem(
                title: "Paper-record reconciliation failed",
                detail: exception.GetBaseException().Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}
