using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VentureHerdManager.Api.Data;
using VentureHerdManager.Api.Models;

namespace VentureHerdManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DryOffEventsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public DryOffEventsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("animal/{animalId}")]
    public async Task<ActionResult<List<DryOffEvent>>> GetByAnimal(int animalId)
    {
        return await _context.DryOffEvents
            .Where(d => d.AnimalId == animalId)
            .OrderByDescending(d => d.DryOffDate)
            .ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<DryOffEvent>> Create(DryOffEvent dryOff)
    {
        var animal = await _context.Animals
            .FirstOrDefaultAsync(a => a.AnimalId == dryOff.AnimalId);

        if (animal == null)
        {
            return NotFound($"Animal {dryOff.AnimalId} was not found.");
        }

        _context.DryOffEvents.Add(dryOff);

        animal.AnimalStage = AnimalStage.Dry;

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetByAnimal),
            new { animalId = dryOff.AnimalId },
            dryOff
        );
    }
}