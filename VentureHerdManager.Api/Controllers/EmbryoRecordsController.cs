using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VentureHerdManager.Api.Data;
using VentureHerdManager.Api.Models;

namespace VentureHerdManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmbryoRecordsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public EmbryoRecordsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<EmbryoRecord>>> GetAll()
    {
        return await _context.EmbryoRecords
            .AsNoTracking()
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<EmbryoRecord>> GetById(int id)
    {
        var record = await _context.EmbryoRecords.FindAsync(id);
        if (record == null)
        {
            return NotFound();
        }

        return record;
    }

    [HttpPost]
    public async Task<ActionResult<EmbryoRecord>> Create(EmbryoRecord record)
    {
        _context.EmbryoRecords.Add(record);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = record.EmbryoRecordId }, record);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, EmbryoRecord record)
    {
        if (id != record.EmbryoRecordId)
        {
            return BadRequest("ID mismatch.");
        }

        var existing = await _context.EmbryoRecords.FindAsync(id);
        if (existing == null)
        {
            return NotFound();
        }

        existing.Code = record.Code;
        existing.Sire = record.Sire;
        existing.Donor = record.Donor;
        existing.Grade = record.Grade;
        existing.Status = record.Status;
        existing.RecipientAnimalId = record.RecipientAnimalId;
        existing.ImplantDate = record.ImplantDate;
        existing.LinkedBreedingNote = record.LinkedBreedingNote;
        existing.FailureNotes = record.FailureNotes;
        existing.Notes = record.Notes;
        existing.CollectionLocation = record.CollectionLocation;
        existing.StorageLocation = record.StorageLocation;
        existing.UpdatedBy = record.UpdatedBy;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var record = await _context.EmbryoRecords.FindAsync(id);
        if (record == null)
        {
            return NotFound();
        }

        _context.EmbryoRecords.Remove(record);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
