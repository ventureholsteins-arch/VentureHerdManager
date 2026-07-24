using Microsoft.AspNetCore.Mvc;

namespace VentureHerdManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AppearanceController : ControllerBase
{
    [HttpGet]
    public IActionResult GetAppearance()
    {
        return Ok(new { message = "Appearance settings are disabled." });
    }
}
