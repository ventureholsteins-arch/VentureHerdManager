using Microsoft.AspNetCore.Mvc;
using VentureHerdManager.Api.Services;

namespace VentureHerdManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly DashboardService _dashboardService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        DashboardService dashboardService,
        ILogger<DashboardController> logger)
    {
        _dashboardService = dashboardService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetDashboard([FromQuery] int dueDays = 30)
    {
        try
        {
            return Ok(await _dashboardService.GetDashboardAsync(dueDays));
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Detailed dashboard failed; returning live herd totals.");
            return Ok(await _dashboardService.GetDashboardFallbackAsync());
        }
    }
}
