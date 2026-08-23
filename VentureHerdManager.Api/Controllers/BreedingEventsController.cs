using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VentureHerdManager.Api.Data;
using VentureHerdManager.Api.Models;
using VentureHerdManager.Api.Services;

namespace VentureHerdManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BreedingEventsController : ControllerBase
{
    private static string NormalizeSireName(string? value) => string.IsNullOrWhiteSpace(value) ? "Service information pending" : string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    private readonly ApplicationDbContext _context;
    private readonly ILogger<BreedingEventsController> _logger;

    public BreedingEventsController(
        ApplicationDbContext context,
        ILogger<BreedingEventsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("animal/{animalId}")]
    public async Task<ActionResult<List<BreedingEvent>>> GetByAnimal(int animalId)
    {
        return await _context.BreedingEvents
            .Where(b => b.AnimalId == animalId)
            .OrderByDescending(b => b.BreedingDate)
            .ToListAsync();
    }

    [HttpGet("latest-status")]
    public async Task<ActionResult<List<LatestPregnancyStatusDto>>> GetLatestStatusByAnimal()
    {
        var latest = await _context.BreedingEvents
            .AsNoTracking()
            .CurrentReproductiveEvents(_context)
            .Select(b => new LatestPregnancyStatusDto
            {
                AnimalId = b.AnimalId,
                PregnancyStatus = b.PregnancyStatus
            })
            .ToListAsync();

        return Ok(latest);
    }

    [HttpPost]
    public async Task<ActionResult<BreedingEvent>> Create(BreedingEvent breeding)
    {
        breeding.SireUsed = NormalizeSireName(breeding.SireUsed);
        var existing = await _context.BreedingEvents
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate =>
                candidate.AnimalId == breeding.AnimalId
                && candidate.BreedingDate == breeding.BreedingDate
                && candidate.BreedingType == breeding.BreedingType
                && candidate.SireUsed == breeding.SireUsed);
        if (existing != null)
        {
            Response.Headers["X-Duplicate-Prevented"] = "true";
            return Ok(existing);
        }

        await ReproductiveEventRules.ClosePriorServiceAsync(
            _context,
            breeding.AnimalId,
            breeding.BreedingDate,
            "a new breeding");
        var isEmbryoTransfer =
            breeding.BreedingType == BreedingType.EmbryoTransfer;
        var expectedDueDate = breeding.BreedingDate.AddDays(
            isEmbryoTransfer
                ? ReproductiveEventRules.EmbryoTransferGestationDays
                : ReproductiveEventRules.StandardGestationDays);
        breeding.ExpectedDueDate = expectedDueDate;
        breeding.PregnancyCheckDueDate = breeding.BreedingDate.AddDays(
            isEmbryoTransfer
                ? ReproductiveEventRules.PregnancyCheckAfterTransferDays
                : ReproductiveEventRules.PregnancyCheckAfterBreedingDays);
        breeding.RecommendedDryOffDate = expectedDueDate.AddDays(
            -ReproductiveEventRules.DryPeriodDays);
        breeding.CloseUpDate = expectedDueDate.AddDays(
            -ReproductiveEventRules.CloseUpDays);

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

        var linkedEmbryo = await _context.EmbryoRecords
            .FirstOrDefaultAsync(e =>
                e.BreedingEventId == breeding.BreedingEventId);
        var isEmbryoTransfer =
            breeding.BreedingType == BreedingType.EmbryoTransfer
            || linkedEmbryo != null;

        await using var transaction = _context.Database.IsRelational()
            ? await _context.Database.BeginTransactionAsync()
            : null;
        var checkedAt = DateTime.UtcNow;

        ReproductiveEventRules.ApplyPregnancyStatus(
            breeding,
            status,
            isEmbryoTransfer,
            checkedAt);

        if (linkedEmbryo != null)
        {
            ReproductiveEventRules.SynchronizeEmbryoOutcome(
                linkedEmbryo,
                status,
                linkedEmbryo.FailureNotes);
        }

        try
        {
            await _context.SaveChangesAsync();
            if (transaction != null)
            {
                await transaction.CommitAsync();
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Pregnancy status {PregnancyStatus} and linked embryo synchronization failed for breeding {BreedingEventId}.",
                status,
                breedingEventId);
            if (transaction != null)
            {
                await transaction.RollbackAsync();
            }
            return Problem(
                title: "Pregnancy result was not saved.",
                detail: exception.GetBaseException().Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }

        return NoContent();
    }

    [HttpPut("{breedingEventId}")]
    public async Task<IActionResult> Update(
        int breedingEventId,
        [FromBody] UpdateBreedingEventRequest request)
    {
        var breeding = await _context.BreedingEvents
            .FirstOrDefaultAsync(b => b.BreedingEventId == breedingEventId);

        if (breeding == null)
        {
            return NotFound();
        }

        breeding.BreedingDate = request.BreedingDate;
        breeding.SireUsed = NormalizeSireName(request.SireUsed);
        breeding.BreedingType = request.BreedingType;
        breeding.PregnancyStatus = request.PregnancyStatus;
        breeding.Notes = request.Notes;
        var linkedEmbryo = await _context.EmbryoRecords
            .FirstOrDefaultAsync(e =>
                e.BreedingEventId == breeding.BreedingEventId);
        var isEmbryoTransfer =
            request.BreedingType == BreedingType.EmbryoTransfer
            || linkedEmbryo != null;
        breeding.PregnancyCheckDueDate = request.BreedingDate.AddDays(
            isEmbryoTransfer
                ? ReproductiveEventRules.PregnancyCheckAfterTransferDays
                : ReproductiveEventRules.PregnancyCheckAfterBreedingDays);
        ReproductiveEventRules.ApplyPregnancyStatus(
            breeding,
            request.PregnancyStatus,
            isEmbryoTransfer,
            breeding.PregnancyCheckDate);
        breeding.UpdatedBy = request.UpdatedBy;
        breeding.UpdatedAt = DateTime.UtcNow;

        if (linkedEmbryo != null)
        {
            linkedEmbryo.RecipientAnimalId = breeding.AnimalId;
            linkedEmbryo.ImplantDate =
                DateOnly.FromDateTime(request.BreedingDate);
            linkedEmbryo.Mating = request.SireUsed;
            ReproductiveEventRules.SynchronizeEmbryoOutcome(
                linkedEmbryo,
                request.PregnancyStatus,
                linkedEmbryo.FailureNotes);
        }

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{breedingEventId}")]
    public async Task<IActionResult> Delete(int breedingEventId)
    {
        var breeding = await _context.BreedingEvents
            .FirstOrDefaultAsync(b => b.BreedingEventId == breedingEventId);

        if (breeding == null)
        {
            return NotFound();
        }

        var linkedEmbryo = await _context.EmbryoRecords
            .FirstOrDefaultAsync(e => e.BreedingEventId == breedingEventId);
        if (linkedEmbryo != null)
        {
            return Conflict(
                "This breeding is linked to an embryo. Use Undo Implant so the transfer history is preserved.");
        }

        _context.BreedingEvents.Remove(breeding);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

public class UpdateBreedingEventRequest
{
    public DateTime BreedingDate { get; set; }

    public string SireUsed { get; set; } = string.Empty;

    public BreedingType BreedingType { get; set; }

    public PregnancyStatus PregnancyStatus { get; set; }

    public string? Notes { get; set; }

    public string? UpdatedBy { get; set; }
}

public class LatestPregnancyStatusDto
{
    public int AnimalId { get; set; }

    public PregnancyStatus PregnancyStatus { get; set; }
}
