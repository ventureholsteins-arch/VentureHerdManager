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
        breeding.SireUsed = string.IsNullOrWhiteSpace(breeding.SireUsed)
            ? "Service information pending"
            : breeding.SireUsed.Trim();
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

        ReproductiveEventRules.ApplyPregnancyStatus(
            breeding,
            status,
            isEmbryoTransfer,
            DateTime.UtcNow);

        if (linkedEmbryo != null)
        {
            ReproductiveEventRules.SynchronizeEmbryoOutcome(
                linkedEmbryo,
                status,
                linkedEmbryo.FailureNotes);
        }

        await _context.SaveChangesAsync();

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
        breeding.SireUsed = string.IsNullOrWhiteSpace(request.SireUsed)
            ? "Service information pending"
            : request.SireUsed.Trim();
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
