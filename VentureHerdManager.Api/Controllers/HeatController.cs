using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VentureHerdManager.Api.Data;
using VentureHerdManager.Api.Models;
using VentureHerdManager.Api.Services;

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

    [HttpGet("recent-recipients")]
    public async Task<IActionResult> GetRecentRecipients(
        [FromQuery] int minDays = 6,
        [FromQuery] int maxDays = 8)
    {
        var today = DateTime.UtcNow.Date;
        var start = today.AddDays(-maxDays);
        var end = today.AddDays(-minDays + 1);

        var rows = await (
            from heat in _context.HeatEvents.AsNoTracking()
            join animal in _context.Animals.AsNoTracking()
                on heat.AnimalId equals animal.AnimalId
            where heat.HeatDateTime >= start
                && heat.HeatDateTime < end
                && animal.AnimalStatus == AnimalStatus.Active
                && animal.Sex == AnimalSex.Female
            orderby heat.HeatDateTime descending
            select new
            {
                animal.AnimalId,
                AnimalName = animal.BarnName ?? animal.RegisteredName
                    ?? $"Animal #{animal.AnimalId}",
                heat.HeatDateTime,
                DaysSinceHeat = (today - heat.HeatDateTime.Date).Days
            })
            .ToListAsync();

        return Ok(rows
            .GroupBy(row => row.AnimalId)
            .Select(group => group.First())
            .ToList());
    }

    [HttpPost]
    public async Task<ActionResult<HeatEvent>> Create(HeatEvent heatEvent)
    {
        var duplicateWindowStart = heatEvent.HeatDateTime.AddMinutes(-2);
        var duplicateWindowEnd = heatEvent.HeatDateTime.AddMinutes(2);
        var existing = await _context.HeatEvents
            .AsNoTracking()
            .FirstOrDefaultAsync(h =>
                h.AnimalId == heatEvent.AnimalId
                && h.HeatDateTime >= duplicateWindowStart
                && h.HeatDateTime <= duplicateWindowEnd
                && h.HeatStrength == heatEvent.HeatStrength
                && h.StandingHeat == heatEvent.StandingHeat
                && h.Notes == heatEvent.Notes);
        if (existing != null)
        {
            Response.Headers["X-Duplicate-Prevented"] = "true";
            return Ok(existing);
        }

        await ReproductiveEventRules.ClosePriorServiceAsync(
            _context,
            heatEvent.AnimalId,
            heatEvent.HeatDateTime,
            "a new heat");
        _context.HeatEvents.Add(heatEvent);
        await _context.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(heatEvent.PictureUrl))
        {
            _context.AnimalPhotos.Add(new AnimalPhoto
            {
                AnimalId = heatEvent.AnimalId,
                PhotoUrl = heatEvent.PictureUrl,
                PhotoType = AnimalPhotoType.Heat,
                RelatedEventId = heatEvent.HeatEventId,
                RelatedEventType = nameof(HeatEvent),
                Caption = "Heat event photo",
                CreatedBy = heatEvent.CreatedBy
            });

            await _context.SaveChangesAsync();
        }

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
