using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VentureHerdManager.Api.Data;
using VentureHerdManager.Api.Models;

namespace VentureHerdManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BreedingEventsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public BreedingEventsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("animal/{animalId}")]
    public async Task<ActionResult<List<BreedingEvent>>> GetByAnimal(int animalId)
    {
        return await _context.BreedingEvents
            .Where(b => b.AnimalId == animalId)
            .OrderByDescending(b => b.BreedingDate)
            .ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<BreedingEvent>> Create(BreedingEvent breeding)
    {
        breeding.ExpectedDueDate = breeding.BreedingDate.AddDays(280);
        breeding.PregnancyCheckDueDate = breeding.BreedingDate.AddDays(30);

        _context.BreedingEvents.Add(breeding);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetByAnimal),
            new { animalId = breeding.AnimalId },
            breeding);
    }

    [HttpPut("{breedingEventId}/pregnancy-status")]
    public async Task<IActionResult> UpdatePregnancyStatus(
        int breedingEventId,
        [FromBody] PregnancyStatus status)
    {
        var breeding = await _context.BreedingEvents
            .FirstOrDefaultAsync(b => b.BreedingEventId == breedingEventId);

        if (breeding == null)
        {
            return NotFound();
        }

        breeding.PregnancyStatus = status;

        await _context.SaveChangesAsync();

        return NoContent();
    }
}