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

    [HttpPost("{id}/implant")]
    public async Task<IActionResult> Implant(
        int id,
        [FromBody] ImplantEmbryoRequest request)
    {
        var record = await _context.EmbryoRecords.FindAsync(id);
        var recipient = await _context.Animals.FindAsync(request.RecipientAnimalId);

        if (record == null || recipient == null)
        {
            return NotFound();
        }

        if (record.Status is EmbryoStatus.Implanted
            or EmbryoStatus.Successful
            or EmbryoStatus.Failed)
        {
            return BadRequest("This embryo is no longer available.");
        }

        record.RecipientAnimalId = recipient.AnimalId;
        record.ImplantDate = request.ImplantDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        record.Status = EmbryoStatus.Implanted;
        record.LinkedBreedingNote =
            $"Embryo implanted in {recipient.BarnName ?? recipient.RegisteredName}.";
        record.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(record);
    }

    [HttpPost("{id}/outcome")]
    public async Task<IActionResult> RecordOutcome(
        int id,
        [FromBody] EmbryoOutcomeRequest request)
    {
        var record = await _context.EmbryoRecords.FindAsync(id);
        if (record == null)
        {
            return NotFound();
        }

        if (record.Status != EmbryoStatus.Implanted
            || !record.RecipientAnimalId.HasValue
            || !record.ImplantDate.HasValue)
        {
            return BadRequest("An implanted embryo and recipient are required.");
        }

        var implantDate = record.ImplantDate.Value.ToDateTime(TimeOnly.MinValue);
        var status = request.Successful
            ? PregnancyStatus.Pregnant
            : PregnancyStatus.Open;

        _context.BreedingEvents.Add(new BreedingEvent
        {
            AnimalId = record.RecipientAnimalId.Value,
            BreedingDate = implantDate,
            SireUsed = record.Sire ?? record.Code ?? "Embryo transfer",
            BreedingType = BreedingType.EmbryoTransfer,
            PregnancyStatus = status,
            PregnancyCheckDate = DateTime.UtcNow,
            PregnancyCheckDueDate = implantDate.AddDays(35),
            ExpectedDueDate = request.Successful ? implantDate.AddDays(280) : null,
            RecommendedDryOffDate = request.Successful ? implantDate.AddDays(220) : null,
            CloseUpDate = request.Successful ? implantDate.AddDays(259) : null,
            Notes = request.Successful
                ? $"Confirmed pregnant from embryo {record.Code ?? record.EmbryoRecordId.ToString()}."
                : $"Embryo {record.Code ?? record.EmbryoRecordId.ToString()} did not establish a pregnancy.",
            CreatedBy = "Embryo workflow",
            UpdatedBy = "Embryo workflow"
        });

        record.Status = request.Successful
            ? EmbryoStatus.Successful
            : EmbryoStatus.Failed;
        record.FailureNotes = request.Successful ? null : request.Notes;
        record.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(record);
    }
}

public class ImplantEmbryoRequest
{
    public int RecipientAnimalId { get; set; }
    public DateOnly? ImplantDate { get; set; }
}

public class EmbryoOutcomeRequest
{
    public bool Successful { get; set; }
    public string? Notes { get; set; }
}
