using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VentureHerdManager.Api.Data;
using VentureHerdManager.Api.Models;

namespace VentureHerdManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmbryoRecordsController : ControllerBase
{
    private const int EmbryoAgeAtTransferDays = 7;
    private const int GestationDays = 280;
    private const int PregnancyCheckAfterTransferDays = 28;
    private const int DryPeriodDays = 60;
    private const int CloseUpDays = 21;

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

    [HttpGet("recipient/{animalId:int}")]
    public async Task<ActionResult<List<EmbryoRecord>>> GetForRecipient(int animalId)
    {
        return await _context.EmbryoRecords
            .AsNoTracking()
            .Where(e => e.RecipientAnimalId == animalId)
            .OrderByDescending(e => e.ImplantDate)
            .ThenByDescending(e => e.CreatedAt)
            .ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<EmbryoRecord>> Create(EmbryoRecord record)
    {
        NormalizeNewRecord(record);
        _context.EmbryoRecords.Add(record);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = record.EmbryoRecordId }, record);
    }

    [HttpPost("batch")]
    public async Task<ActionResult<List<EmbryoRecord>>> CreateBatch(
        [FromBody] CreateEmbryoBatchRequest request)
    {
        if (request.Quantity is < 1 or > 100)
        {
            return BadRequest("Quantity must be between 1 and 100.");
        }

        if (request.Embryo == null)
        {
            return BadRequest("Embryo details are required.");
        }

        var records = Enumerable.Range(0, request.Quantity)
            .Select(_ => new EmbryoRecord
            {
                Code = request.Embryo.Code,
                Sire = request.Embryo.Sire,
                Donor = request.Embryo.Donor,
                Mating = request.Embryo.Mating,
                DonorAnimalId = request.Embryo.DonorAnimalId,
                Grade = request.Embryo.Grade,
                GroupName = request.Embryo.GroupName,
                Status = EmbryoStatus.InStorage,
                RecipientAnimalId = null,
                ImplantDate = null,
                BreedingEventId = null,
                LinkedBreedingNote = request.Embryo.LinkedBreedingNote,
                FailureNotes = null,
                Notes = request.Embryo.Notes,
                CollectionLocation = request.Embryo.CollectionLocation,
                StorageLocation = request.Embryo.StorageLocation,
                CreatedBy = request.Embryo.CreatedBy,
                UpdatedBy = request.Embryo.UpdatedBy
            })
            .ToList();

        foreach (var record in records)
        {
            NormalizeNewRecord(record);
        }

        _context.EmbryoRecords.AddRange(records);
        await _context.SaveChangesAsync();

        return Ok(records);
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
        existing.Mating = record.Mating;
        existing.DonorAnimalId = record.DonorAnimalId;
        existing.Grade = record.Grade;
        existing.GroupName = record.GroupName;
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

        if (existing.BreedingEventId.HasValue
            && existing.RecipientAnimalId.HasValue
            && existing.ImplantDate.HasValue)
        {
            var breeding = await _context.BreedingEvents
                .FindAsync(existing.BreedingEventId.Value);
            if (breeding != null)
            {
                ApplyTransferDetails(breeding, existing);
            }
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("group")]
    public async Task<IActionResult> SetGroup([FromBody] SetEmbryoGroupRequest request)
    {
        var ids = request.EmbryoRecordIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return BadRequest("Select at least one embryo.");
        }

        var records = await _context.EmbryoRecords
            .Where(record => ids.Contains(record.EmbryoRecordId))
            .ToListAsync();

        if (records.Count != ids.Count)
        {
            return BadRequest("One or more selected embryos could not be found.");
        }

        var groupName = string.IsNullOrWhiteSpace(request.GroupName)
            ? null
            : request.GroupName.Trim();

        foreach (var record in records)
        {
            record.GroupName = groupName;
            record.UpdatedAt = DateTime.UtcNow;
        }

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

        if (record.BreedingEventId.HasValue)
        {
            var breeding = await _context.BreedingEvents
                .FindAsync(record.BreedingEventId.Value);
            if (breeding != null)
            {
                _context.BreedingEvents.Remove(breeding);
            }
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

        var implantDate = request.ImplantDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        record.RecipientAnimalId = recipient.AnimalId;
        record.ImplantDate = implantDate;
        record.Status = EmbryoStatus.Implanted;
        record.LinkedBreedingNote =
            $"Embryo implanted in {recipient.BarnName ?? recipient.RegisteredName}.";
        record.UpdatedAt = DateTime.UtcNow;

        var breeding = new BreedingEvent();
        ApplyTransferDetails(breeding, record);
        breeding.CreatedBy = "Embryo workflow";
        _context.BreedingEvents.Add(breeding);

        await _context.SaveChangesAsync();
        record.BreedingEventId = breeding.BreedingEventId;
        await _context.SaveChangesAsync();
        return Ok(record);
    }

    [HttpPost("{id}/assign")]
    public async Task<IActionResult> Assign(
        int id,
        [FromBody] AssignEmbryoRequest request)
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
        record.Status = EmbryoStatus.Assigned;
        record.LinkedBreedingNote =
            $"Reserved after heat for {recipient.BarnName ?? recipient.RegisteredName}.";
        record.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(record);
    }

    [HttpPost("{id}/undo-implant")]
    public async Task<IActionResult> UndoImplant(int id)
    {
        var record = await _context.EmbryoRecords.FindAsync(id);
        if (record == null) return NotFound();
        if (record.Status is EmbryoStatus.Successful or EmbryoStatus.Failed)
            return BadRequest("Undo the pregnancy outcome before removing this implant.");

        if (record.BreedingEventId.HasValue)
        {
            var breeding = await _context.BreedingEvents.FindAsync(record.BreedingEventId.Value);
            if (breeding != null) _context.BreedingEvents.Remove(breeding);
        }

        record.Status = EmbryoStatus.InStorage;
        record.RecipientAnimalId = null;
        record.ImplantDate = null;
        record.BreedingEventId = null;
        record.LinkedBreedingNote = "Implant entry was corrected and returned to inventory.";
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

        if (record.Status is not (
                EmbryoStatus.Implanted
                or EmbryoStatus.Successful
                or EmbryoStatus.Failed)
            || !record.RecipientAnimalId.HasValue
            || !record.ImplantDate.HasValue)
        {
            return BadRequest("An implanted embryo and recipient are required.");
        }

        var implantDate = record.ImplantDate.Value.ToDateTime(TimeOnly.MinValue);
        var status = request.Successful
            ? PregnancyStatus.Pregnant
            : PregnancyStatus.Open;

        var breeding = record.BreedingEventId.HasValue
            ? await _context.BreedingEvents.FindAsync(record.BreedingEventId.Value)
            : null;

        if (breeding == null)
        {
            breeding = new BreedingEvent { CreatedBy = "Embryo workflow" };
            ApplyTransferDetails(breeding, record);
            _context.BreedingEvents.Add(breeding);
        }

        breeding.PregnancyStatus = status;
        breeding.PregnancyCheckDate = DateTime.UtcNow;
        var expectedDueDate =
            implantDate.AddDays(GestationDays - EmbryoAgeAtTransferDays);
        breeding.ExpectedDueDate = request.Successful
            ? expectedDueDate
            : null;
        breeding.RecommendedDryOffDate = request.Successful
            ? expectedDueDate.AddDays(-DryPeriodDays)
            : null;
        breeding.CloseUpDate = request.Successful
            ? expectedDueDate.AddDays(-CloseUpDays)
            : null;
        breeding.Notes = request.Successful
            ? $"Confirmed pregnant from embryo {record.Code ?? record.EmbryoRecordId.ToString()}."
            : $"Embryo {record.Code ?? record.EmbryoRecordId.ToString()} did not establish a pregnancy.";
        breeding.UpdatedAt = DateTime.UtcNow;

        record.Status = request.Successful
            ? EmbryoStatus.Successful
            : EmbryoStatus.Failed;
        record.FailureNotes = request.Successful ? null : request.Notes;
        record.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        if (!record.BreedingEventId.HasValue)
        {
            record.BreedingEventId = breeding.BreedingEventId;
            await _context.SaveChangesAsync();
        }
        return Ok(record);
    }

    private static void NormalizeNewRecord(EmbryoRecord record)
    {
        record.Code = Clean(record.Code);
        record.Sire = Clean(record.Sire);
        record.Donor = Clean(record.Donor);
        record.Mating = Clean(record.Mating) ?? BuildEmbryoName(record);
        record.Grade = Clean(record.Grade);
        record.GroupName = Clean(record.GroupName)
            ?? BuildEmbryoName(record);
        record.Status = EmbryoStatus.InStorage;
        record.RecipientAnimalId = null;
        record.ImplantDate = null;
        record.BreedingEventId = null;
        record.CreatedAt = DateTime.UtcNow;
        record.UpdatedAt = DateTime.UtcNow;
    }

    private static string? BuildEmbryoName(EmbryoRecord record)
    {
        if (record.Donor == null && record.Sire == null) return null;
        return $"{record.Donor ?? "Unknown dam"} x {record.Sire ?? "Unknown sire"}";
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void ApplyTransferDetails(
        BreedingEvent breeding,
        EmbryoRecord record)
    {
        var implantDate = record.ImplantDate!.Value.ToDateTime(TimeOnly.MinValue);
        var dueDate = implantDate.AddDays(GestationDays - EmbryoAgeAtTransferDays);

        breeding.AnimalId = record.RecipientAnimalId!.Value;
        breeding.BreedingDate = implantDate;
        breeding.SireUsed = record.Mating ?? record.Sire ?? record.Code ?? "Embryo transfer";
        breeding.BreedingType = BreedingType.EmbryoTransfer;
        breeding.PregnancyCheckDueDate =
            implantDate.AddDays(PregnancyCheckAfterTransferDays);
        breeding.ExpectedDueDate = dueDate;
        breeding.RecommendedDryOffDate = dueDate.AddDays(-DryPeriodDays);
        breeding.CloseUpDate = dueDate.AddDays(-CloseUpDays);
        breeding.Notes =
            $"Embryo transfer #{record.EmbryoRecordId}: {record.Code ?? "No code"}.";
        breeding.UpdatedBy = "Embryo workflow";
        breeding.UpdatedAt = DateTime.UtcNow;
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

public class AssignEmbryoRequest
{
    public int RecipientAnimalId { get; set; }
}

public class CreateEmbryoBatchRequest
{
    public int Quantity { get; set; } = 1;
    public EmbryoRecord? Embryo { get; set; }
}

public class SetEmbryoGroupRequest
{
    public List<int> EmbryoRecordIds { get; set; } = [];
    public string? GroupName { get; set; }
}
