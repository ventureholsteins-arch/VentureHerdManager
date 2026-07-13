using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VentureHerdManager.Api.Data;
using VentureHerdManager.Api.Models;

namespace VentureHerdManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnimalNotesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public AnimalNotesController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("animal/{animalId}")]
    public async Task<ActionResult<List<AnimalNote>>> GetByAnimal(int animalId)
    {
        return await _context.AnimalNotes
            .Where(n => n.AnimalId == animalId)
            .OrderByDescending(n => n.NoteDate)
            .ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<AnimalNote>> Create(AnimalNote note)
    {
        var animal = await _context.Animals
            .FirstOrDefaultAsync(a => a.AnimalId == note.AnimalId);

        if (animal == null)
        {
            return NotFound($"Animal {note.AnimalId} was not found.");
        }

        _context.AnimalNotes.Add(note);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetByAnimal),
            new { animalId = note.AnimalId },
            note
        );
    }
}