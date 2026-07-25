using Microsoft.AspNetCore.Mvc;

namespace VentureHerdManager.Api.Controllers;

/// <summary>
/// CRITICAL: Demo functionality is disabled to prevent production data loss.
/// 
/// REASON: The demo environment was accidentally connected to the production database.
/// The reset endpoint deleted photos, notes, classifications, and events from production.
/// 
/// SOLUTION: Demo mode requires a completely separate database.
/// Until a separate demo database is configured, ALL demo endpoints return 403 Forbidden.
/// 
/// FUTURE IMPLEMENTATION:
/// 1. Create separate Azure SQL database for demo-only data
/// 2. Add DemoConnection string to appsettings.json
/// 3. Update Program.cs to route demo mode requests to the demo database
/// 4. Re-enable reset endpoint ONLY when DemoMode:Enabled is true AND using DemoConnection
/// 5. Add comprehensive tests to prevent demo from modifying production data
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DemoController : ControllerBase
{
    [HttpPost("reset")]
    public ActionResult<object> Reset()
    {
        return StatusCode(403, new 
        { 
            error = "Demo reset endpoint is permanently disabled.",
            reason = "Demo mode requires a separate database to prevent production data loss.",
            action = "Contact administrator to set up demo database."
        });
    }

    [HttpGet("status")]
    public ActionResult<object> Status()
    {
        return StatusCode(403, new 
        { 
            error = "Demo is disabled.",
            reason = "Demo functionality requires database isolation."
        });
    }
}
