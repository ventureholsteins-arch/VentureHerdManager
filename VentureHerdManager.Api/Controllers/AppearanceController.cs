using Microsoft.AspNetCore.Mvc;

namespace VentureHerdManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AppearanceController : ControllerBase
{
    [HttpGet]
    public IActionResult GetAppearance()
    {
        // Return default appearance settings
        return Ok(new
        {
            appearanceSettingId = 0,
            farmName = "Venture Herd Manager",
            logoUrl = "/farm-logo.png",
            backgroundImageUrl = "/Seashell_cow.jpg",
            backgroundOpacity = 0.15,
            overlayOpacity = 0.85,
            theme = "light",
            accentColor = "#31572c",
            updatedAt = DateTime.UtcNow.ToString("O")
        });
    }
}
