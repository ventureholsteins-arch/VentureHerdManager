using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VentureHerdManager.Api.Data;
using VentureHerdManager.Api.Models;

namespace VentureHerdManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CalvingEventsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public CalvingEventsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("animal/{animalId}")]
    public async Task<ActionResult<List<CalvingEvent>>> GetByAnimal(int animalId)
    {
        return await _context.CalvingEvents
            .Where(c => c.AnimalId == animalId)
            .OrderByDescending(c => c.CalvingDate)
            .ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<CalvingEvent>> Create(CalvingEvent calving)
    {
        var animal = await _context.Animals
            .FirstOrDefaultAsync(a => a.AnimalId == calving.AnimalId);

        if (animal == null)
        {
            return NotFound($"Animal {calving.AnimalId} was not found.");
        }

        _context.CalvingEvents.Add(calving);

        animal.AnimalStage = AnimalStage.Milking;

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetByAnimal),
            new { animalId = calving.AnimalId },
            calving);
    }
}