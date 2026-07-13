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
}