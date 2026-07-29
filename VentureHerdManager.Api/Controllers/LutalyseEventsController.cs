using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VentureHerdManager.Api.Data;
using VentureHerdManager.Api.Models;

namespace VentureHerdManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LutalyseEventsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public LutalyseEventsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("animal/{animalId}")]
    public async Task<ActionResult<List<LutalyseEvent>>> GetByAnimal(int animalId)
    {
        return await _context.LutalyseEvents
            .Where(l => l.AnimalId == animalId)
            .OrderByDescending(l => l.AdministrationDate)
            .ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<LutalyseEvent>> Create(LutalyseEvent lutalyseEvent)
    {
        _context.LutalyseEvents.Add(lutalyseEvent);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetByAnimal),
            new { animalId = lutalyseEvent.AnimalId },
            lutalyseEvent);
    }

    [HttpPut("{lutalyseEventId}")]
    public async Task<IActionResult> Update(
        int lutalyseEventId,
        [FromBody] UpdateLutalyseEventRequest request)
    {
        var lutalyseEvent = await _context.LutalyseEvents
            .FirstOrDefaultAsync(l => l.LutalyseEventId == lutalyseEventId);

        if (lutalyseEvent == null)
        {
            return NotFound();
        }

        lutalyseEvent.AdministrationDate = request.AdministrationDate;
        lutalyseEvent.ExpectedHeatWatchStart = request.ExpectedHeatWatchStart;
        lutalyseEvent.ExpectedHeatWatchEnd = request.ExpectedHeatWatchEnd;
        lutalyseEvent.HeatObserved = request.HeatObserved;
        lutalyseEvent.Notes = request.Notes;
        lutalyseEvent.UpdatedBy = request.UpdatedBy;
        lutalyseEvent.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{lutalyseEventId}")]
    public async Task<IActionResult> Delete(int lutalyseEventId)
    {
        var lutalyseEvent = await _context.LutalyseEvents
            .FirstOrDefaultAsync(l => l.LutalyseEventId == lutalyseEventId);

        if (lutalyseEvent == null)
        {
            return NotFound();
        }

        _context.LutalyseEvents.Remove(lutalyseEvent);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

public class UpdateLutalyseEventRequest
{
    public DateTime AdministrationDate { get; set; }

    public DateTime ExpectedHeatWatchStart { get; set; }

    public DateTime ExpectedHeatWatchEnd { get; set; }

    public bool HeatObserved { get; set; }

    public string? Notes { get; set; }

    public string? UpdatedBy { get; set; }
}
