using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VentureHerdManager.Api.Data;
using VentureHerdManager.Api.Models;

namespace VentureHerdManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HeatEventsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public HeatEventsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("animal/{animalId}")]
    public async Task<ActionResult<List<HeatEvent>>> GetByAnimal(int animalId)
    {
        return await _context.HeatEvents
            .Where(h => h.AnimalId == animalId)
            .OrderByDescending(h => h.HeatDateTime)
            .ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<HeatEvent>> Create(HeatEvent heatEvent)
    {
        _context.HeatEvents.Add(heatEvent);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetByAnimal), new { animalId = heatEvent.AnimalId }, heatEvent);
    }

    [HttpPut("{heatEventId}")]
    public async Task<IActionResult> Update(
        int heatEventId,
        [FromBody] UpdateHeatEventRequest request)
    {
        var heatEvent = await _context.HeatEvents
            .FirstOrDefaultAsync(h => h.HeatEventId == heatEventId);

        if (heatEvent == null)
        {
            return NotFound();
        }

        heatEvent.HeatDateTime = request.HeatDateTime;
        heatEvent.Notes = request.Notes;
        heatEvent.PictureUrl = request.PictureUrl;
        heatEvent.UpdatedBy = request.UpdatedBy;
        heatEvent.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{heatEventId}")]
    public async Task<IActionResult> Delete(int heatEventId)
    {
        var heatEvent = await _context.HeatEvents
            .FirstOrDefaultAsync(h => h.HeatEventId == heatEventId);

        if (heatEvent == null)
        {
            return NotFound();
        }

        _context.HeatEvents.Remove(heatEvent);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

public class UpdateHeatEventRequest
{
    public DateTime HeatDateTime { get; set; }

    public string? Notes { get; set; }

    public string? PictureUrl { get; set; }

    public string? UpdatedBy { get; set; }
}