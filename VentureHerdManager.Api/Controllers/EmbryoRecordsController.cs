using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VentureHerdManager.Api.Data;
using VentureHerdManager.Api.Models;
using VentureHerdManager.Api.Services;

namespace VentureHerdManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmbryoRecordsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<EmbryoRecordsController> _logger;

    public EmbryoRecordsController(
        ApplicationDbContext context,
        ILogger<EmbryoRecordsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<EmbryoRecordListItem>>> GetAll()
    {
        return await _context.EmbryoRecords
            .AsNoTracking()
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new EmbryoRecordListItem
            {
                EmbryoRecordId = e.EmbryoRecordId,
                Code = e.Code,
                Sire = e.Sire,
                Donor = e.Donor,
                Mating = e.Mating,
                DonorAnimalId = e.DonorAnimalId,
                Grade = e.Grade,
                GroupName = e.GroupName,
                Status = e.Status,
                RecipientAnimalId = e.RecipientAnimalId,
                RecipientName = e.RecipientAnimal == null ? null : e.RecipientAnimal.BarnName ?? e.RecipientAnimal.RegisteredName,
                ImplantDate = e.ImplantDate,
                BreedingEventId = e.BreedingEventId,
                PregnancyStatus = e.BreedingEvent == null ? null : e.BreedingEvent.PregnancyStatus,
                PregnancyCheckDate = e.BreedingEvent == null ? null : e.BreedingEvent.PregnancyCheckDate,
                PregnancyCheckDueDate = e.BreedingEvent == null ? null : e.BreedingEvent.PregnancyCheckDueDate,
                LinkedBreedingNote = e.LinkedBreedingNote,
                FailureNotes = e.FailureNotes,
                Notes = e.Notes,
                CollectionLocation = e.CollectionLocation,
                StorageLocation = e.StorageLocation,
                CreatedAt = e.CreatedAt,
                UpdatedAt = e.UpdatedAt
            })
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

        await using var transaction = _context.Database.IsRelational()
            ? await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable)
            : null;

        var existing = await _context.EmbryoRecords.FindAsync(id);
        if (existing == null)
        {
            return NotFound();
        }

        existing.Code = Clean(record.Code);
        existing.Sire = Clean(record.Sire);
        existing.Donor = Clean(record.Donor);
        existing.Mating = Clean(record.Mating)
            ?? existing.Mating
            ?? BuildEmbryoName(existing);
        existing.DonorAnimalId = record.DonorAnimalId;
        existing.Grade = Clean(record.Grade);
        existing.GroupName = Clean(record.GroupName)
            ?? existing.GroupName
            ?? existing.Mating
            ?? BuildEmbryoName(existing);
        existing.LinkedBreedingNote = Clean(record.LinkedBreedingNote);
        existing.FailureNotes = Clean(record.FailureNotes);
        existing.Notes = Clean(record.Notes);
        existing.CollectionLocation = Clean(record.CollectionLocation);
        existing.StorageLocation = Clean(record.StorageLocation);
        existing.UpdatedBy = Clean(record.UpdatedBy);
        existing.UpdatedAt = DateTime.UtcNow;

        var hasImplantHistory =
            existing.BreedingEventId.HasValue
            || existing.Status is (
                EmbryoStatus.Implanted
                or EmbryoStatus.Successful
                or EmbryoStatus.Failed)
            || existing.ImplantDate.HasValue;
        if (hasImplantHistory)
        {
            var correctedRecipientId =
                record.RecipientAnimalId
                ?? existing.RecipientAnimalId;
            var correctedImplantDate =
                record.ImplantDate
                ?? existing.ImplantDate;
            if (!correctedRecipientId.HasValue
                || !correctedImplantDate.HasValue)
            {
                return BadRequest(
                    "An implanted embryo must keep a recipient and implant date. Use Undo Implant to return it to inventory.");
            }

            var recipientExists = await _context.Animals
                .AnyAsync(animal =>
                    animal.AnimalId == correctedRecipientId.Value);
            if (!recipientExists)
            {
                return BadRequest("The selected recipient does not exist.");
            }

            existing.RecipientAnimalId = correctedRecipientId;
            existing.ImplantDate = correctedImplantDate;

            var breeding = await ResolveLinkedBreedingAsync(existing);
            if (breeding == null)
            {
                breeding = new BreedingEvent
                {
                    CreatedBy = "Embryo correction workflow"
                };
                _context.BreedingEvents.Add(breeding);
                existing.BreedingEventId = null;
                existing.BreedingEvent = breeding;
            }

            ApplyTransferDetails(breeding, existing);
        }

        await _context.SaveChangesAsync();
        if (transaction != null)
        {
            await transaction.CommitAsync();
        }
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
        await using var transaction = _context.Database.IsRelational()
            ? await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable)
            : null;

        var record = await _context.EmbryoRecords.FindAsync(id);
        if (record == null)
        {
            return NotFound();
        }

        if (record.BreedingEventId.HasValue)
        {
            return Conflict(
                "This embryo has implant history. Use Undo Implant first so the breeding history is preserved.");
        }

        if (record.Status is not (
                EmbryoStatus.InStorage or EmbryoStatus.Assigned))
        {
            return Conflict(
                "This embryo has an implant outcome and cannot be deleted without first correcting the implant.");
        }

        _context.EmbryoRecords.Remove(record);
        await _context.SaveChangesAsync();
        if (transaction != null)
        {
            await transaction.CommitAsync();
        }
        return NoContent();
    }

    [HttpPost("{id}/implant")]
    public async Task<IActionResult> Implant(
        int id,
        [FromBody] ImplantEmbryoRequest request)
    {
        await using var transaction = _context.Database.IsRelational()
            ? await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable)
            : null;

        var record = await _context.EmbryoRecords.FindAsync(id);
        var recipient = await _context.Animals.FindAsync(request.RecipientAnimalId);

        if (record == null || recipient == null)
        {
            return NotFound();
        }

        var linkedBreeding = await ResolveLinkedBreedingAsync(record);
        var implantDate = request.ImplantDate
            ?? DateOnly.FromDateTime(DateTime.UtcNow);

        if (record.RecipientAnimalId == recipient.AnimalId
            && record.ImplantDate == implantDate
            && record.Status == EmbryoStatus.Implanted
            && linkedBreeding != null)
        {
            Response.Headers["X-Duplicate-Prevented"] = "true";
            return Ok(record);
        }

        if (record.Status is EmbryoStatus.Implanted
            or EmbryoStatus.Successful
            or EmbryoStatus.Failed
            || linkedBreeding != null)
        {
            return BadRequest("This embryo is no longer available.");
        }

        await ReproductiveEventRules.ClosePriorServiceAsync(
            _context,
            recipient.AnimalId,
            implantDate.ToDateTime(TimeOnly.MinValue),
            "a new embryo implant");
        record.Mating ??= BuildEmbryoName(record);
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
        record.BreedingEvent = breeding;

        await _context.SaveChangesAsync();
        if (transaction != null)
        {
            await transaction.CommitAsync();
        }
        return Ok(record);
    }

    [HttpPost("{id}/assign")]
    public async Task<IActionResult> Assign(
        int id,
        [FromBody] AssignEmbryoRequest request)
    {
        await using var transaction = _context.Database.IsRelational()
            ? await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable)
            : null;

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

        if (record.Status == EmbryoStatus.Assigned
            && record.RecipientAnimalId == recipient.AnimalId)
        {
            Response.Headers["X-Duplicate-Prevented"] = "true";
            return Ok(record);
        }

        record.RecipientAnimalId = recipient.AnimalId;
        record.Status = EmbryoStatus.Assigned;
        record.LinkedBreedingNote =
            $"Reserved after heat for {recipient.BarnName ?? recipient.RegisteredName}.";
        record.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        if (transaction != null)
        {
            await transaction.CommitAsync();
        }
        return Ok(record);
    }

    [HttpPost("{id}/undo-implant")]
    public async Task<IActionResult> UndoImplant(int id)
    {
        await using var transaction = _context.Database.IsRelational()
            ? await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable)
            : null;

        var record = await _context.EmbryoRecords.FindAsync(id);
        if (record == null) return NotFound();

        var breeding = await ResolveLinkedBreedingAsync(record);

        if (breeding != null)
        {
            ReproductiveEventRules.ApplyPregnancyStatus(
                breeding,
                PregnancyStatus.Open,
                true,
                DateTime.UtcNow);
            breeding.PregnancyCheckDueDate = null;
            breeding.Notes = ReproductiveEventRules.AppendNote(
                breeding.Notes,
                $"Implant entry was corrected on {DateTime.UtcNow:d}; the embryo was returned to inventory.");
            breeding.UpdatedBy = "Embryo correction workflow";
        }

        if (breeding == null
            && record.RecipientAnimalId.HasValue
            && record.ImplantDate.HasValue)
        {
            var preservedBreeding = new BreedingEvent
            {
                CreatedBy = "Embryo correction workflow"
            };
            ApplyTransferDetails(preservedBreeding, record);
            ReproductiveEventRules.ApplyPregnancyStatus(
                preservedBreeding,
                PregnancyStatus.Open,
                true,
                DateTime.UtcNow);
            preservedBreeding.PregnancyCheckDueDate = null;
            preservedBreeding.Notes =
                ReproductiveEventRules.AppendNote(
                    preservedBreeding.Notes,
                    $"Unlinked implant entry was corrected on {DateTime.UtcNow:d}; the embryo was returned to inventory.");
            _context.BreedingEvents.Add(preservedBreeding);
        }

        record.Status = EmbryoStatus.InStorage;
        record.RecipientAnimalId = null;
        record.ImplantDate = null;
        record.BreedingEventId = null;
        record.BreedingEvent = null;
        record.LinkedBreedingNote = "Implant entry was corrected and returned to inventory.";
        record.FailureNotes = null;
        record.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        if (transaction != null)
        {
            await transaction.CommitAsync();
        }
        return Ok(record);
    }

    [HttpPost("{id}/outcome")]
    public async Task<IActionResult> RecordOutcome(
        int id,
        [FromBody] EmbryoOutcomeRequest request)
    {
        await using var transaction = _context.Database.IsRelational()
            ? await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable)
            : null;

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

        var status = request.Successful
            ? PregnancyStatus.Pregnant
            : PregnancyStatus.Open;

        var breeding = await ResolveLinkedBreedingAsync(record);

        if (breeding == null)
        {
            breeding = new BreedingEvent { CreatedBy = "Embryo workflow" };
            ApplyTransferDetails(breeding, record);
            _context.BreedingEvents.Add(breeding);
            record.BreedingEventId = null;
            record.BreedingEvent = breeding;
        }

        ReproductiveEventRules.ApplyPregnancyStatus(
            breeding,
            status,
            true,
            DateTime.UtcNow);
        breeding.Notes = ReproductiveEventRules.AppendNote(
            breeding.Notes,
            request.Successful
                ? $"Confirmed pregnant from embryo {record.Code ?? record.EmbryoRecordId.ToString()}."
                : $"Embryo {record.Code ?? record.EmbryoRecordId.ToString()} did not establish a pregnancy.");
        breeding.UpdatedAt = DateTime.UtcNow;

        ReproductiveEventRules.SynchronizeEmbryoOutcome(
            record,
            status,
            request.Notes);

        await _context.SaveChangesAsync();
        if (transaction != null)
        {
            await transaction.CommitAsync();
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
        var dueDate = implantDate.AddDays(
            ReproductiveEventRules.EmbryoTransferGestationDays);

        breeding.AnimalId = record.RecipientAnimalId!.Value;
        breeding.BreedingDate = implantDate;
        breeding.SireUsed = record.Mating
            ?? BuildEmbryoName(record)
            ?? record.Code
            ?? "Embryo transfer";
        breeding.BreedingType = BreedingType.EmbryoTransfer;
        breeding.PregnancyCheckDueDate =
            implantDate.AddDays(
                ReproductiveEventRules.PregnancyCheckAfterTransferDays);
        breeding.ExpectedDueDate = dueDate;
        breeding.RecommendedDryOffDate = dueDate.AddDays(
            -ReproductiveEventRules.DryPeriodDays);
        breeding.CloseUpDate = dueDate.AddDays(
            -ReproductiveEventRules.CloseUpDays);
        breeding.Notes = ReproductiveEventRules.AppendNote(
            breeding.Notes,
            $"Embryo transfer #{record.EmbryoRecordId}: {record.Mating ?? record.Code ?? "No code"}.");
        breeding.UpdatedBy = "Embryo workflow";
        breeding.UpdatedAt = DateTime.UtcNow;

        ReproductiveEventRules.ApplyPregnancyStatus(
            breeding,
            breeding.PregnancyStatus,
            true,
            breeding.PregnancyCheckDate);
    }

    private async Task<BreedingEvent?> ResolveLinkedBreedingAsync(
        EmbryoRecord record)
    {
        if (!record.BreedingEventId.HasValue)
        {
            return null;
        }

        var breeding = await _context.BreedingEvents.FindAsync(
            record.BreedingEventId.Value);
        if (breeding != null)
        {
            return breeding;
        }

        _logger.LogWarning(
            "Embryo record {EmbryoRecordId} referenced missing breeding event {BreedingEventId}. Clearing stale link.",
            record.EmbryoRecordId,
            record.BreedingEventId.Value);

        record.BreedingEventId = null;
        record.BreedingEvent = null;
        record.LinkedBreedingNote = ReproductiveEventRules.AppendNote(
            record.LinkedBreedingNote,
            $"Cleared missing breeding link {DateTime.UtcNow:d}; a replacement breeding event will be created if needed.");
        record.UpdatedAt = DateTime.UtcNow;

        return null;
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

public sealed class EmbryoRecordListItem
{
    public int EmbryoRecordId { get; set; }
    public string? Code { get; set; }
    public string? Sire { get; set; }
    public string? Donor { get; set; }
    public string? Mating { get; set; }
    public int? DonorAnimalId { get; set; }
    public string? Grade { get; set; }
    public string? GroupName { get; set; }
    public EmbryoStatus Status { get; set; }
    public int? RecipientAnimalId { get; set; }
    public string? RecipientName { get; set; }
    public DateOnly? ImplantDate { get; set; }
    public int? BreedingEventId { get; set; }
    public PregnancyStatus? PregnancyStatus { get; set; }
    public DateTime? PregnancyCheckDate { get; set; }
    public DateTime? PregnancyCheckDueDate { get; set; }
    public string? LinkedBreedingNote { get; set; }
    public string? FailureNotes { get; set; }
    public string? Notes { get; set; }
    public string? CollectionLocation { get; set; }
    public string? StorageLocation { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
