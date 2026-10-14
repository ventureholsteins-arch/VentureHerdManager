using Microsoft.AspNetCore.Mvc;
using VentureHerdManager.Api.Services;

namespace VentureHerdManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class SaleCatalogController(SaleCatalogStorageService storage, HerdDataAdminAccess admin) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<SaleCatalogDocument>> Get(CancellationToken ct) => Ok(await storage.ReadAsync(ct));

    [HttpPut]
    public async Task<IActionResult> Put([FromBody] SaleCatalogDocument catalog, CancellationToken ct)
    {
        if (!admin.IsAuthorized(Request)) return Unauthorized("Private herd-manager access is required.");
        if (string.IsNullOrWhiteSpace(catalog.SaleName)) return BadRequest("Sale name is required.");
        catalog.UpdatedAt = DateTime.UtcNow;
        await storage.WriteAsync(catalog, ct);
        return Ok(catalog);
    }
}
