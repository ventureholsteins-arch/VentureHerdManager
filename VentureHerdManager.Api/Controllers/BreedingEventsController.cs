using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VentureHerdManager.Api.Data;
using VentureHerdManager.Api.Models;

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
            .GroupBy(b => b.AnimalId)
            .Select(group => group
                .OrderByDescending(b => b.BreedingDate)
                .ThenByDescending(b => b.BreedingEventId)
                .Select(b => new LatestPregnancyStatusDto
                {
                    AnimalId = b.AnimalId,
                    PregnancyStatus = b.PregnancyStatus
                })
                .First())
            .ToListAsync();

        return Ok(latest);
    }

    [HttpPost]
    public async Task<ActionResult<BreedingEvent>> Create(BreedingEvent breeding)
    {
        breeding.ExpectedDueDate = breeding.BreedingDate.AddDays(280);
        breeding.PregnancyCheckDueDate = breeding.BreedingDate.AddDays(30);

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

        breeding.PregnancyStatus = status;
        breeding.PregnancyCheckDate = DateTime.UtcNow;

        var linkedEmbryo = await _context.EmbryoRecords
            .FirstOrDefaultAsync(e =>
                e.BreedingEventId == breeding.BreedingEventId);
        if (linkedEmbryo != null)
        {
            linkedEmbryo.Status = status == PregnancyStatus.Pregnant
                ? EmbryoStatus.Successful
                : status == PregnancyStatus.Open
                    ? EmbryoStatus.Failed
                    : EmbryoStatus.Implanted;

            if (status == PregnancyStatus.Open)
            {
                breeding.ExpectedDueDate = null;
                breeding.RecommendedDryOffDate = null;
                breeding.CloseUpDate = null;
            }

            linkedEmbryo.UpdatedAt = DateTime.UtcNow;
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
        breeding.SireUsed = request.SireUsed;
        breeding.BreedingType = request.BreedingType;
        breeding.PregnancyStatus = request.PregnancyStatus;
        breeding.Notes = request.Notes;
        var linkedEmbryo = await _context.EmbryoRecords
            .FirstOrDefaultAsync(e =>
                e.BreedingEventId == breeding.BreedingEventId);
        var isEmbryoTransfer =
            request.BreedingType == BreedingType.EmbryoTransfer
            || linkedEmbryo != null;
        var expectedDueDate = request.BreedingDate.AddDays(
            isEmbryoTransfer ? 273 : 280);
        breeding.ExpectedDueDate = request.PregnancyStatus == PregnancyStatus.Open
            ? null
            : expectedDueDate;
        breeding.PregnancyCheckDueDate = request.BreedingDate.AddDays(
            isEmbryoTransfer ? 28 : 30);
        breeding.RecommendedDryOffDate = breeding.ExpectedDueDate?.AddDays(-60);
        breeding.CloseUpDate = breeding.ExpectedDueDate?.AddDays(-21);
        breeding.UpdatedBy = request.UpdatedBy;
        breeding.UpdatedAt = DateTime.UtcNow;

        if (linkedEmbryo != null)
        {
            linkedEmbryo.RecipientAnimalId = breeding.AnimalId;
            linkedEmbryo.ImplantDate =
                DateOnly.FromDateTime(request.BreedingDate);
            linkedEmbryo.Sire = request.SireUsed;
            linkedEmbryo.Status = request.PregnancyStatus switch
            {
                PregnancyStatus.Pregnant => EmbryoStatus.Successful,
                PregnancyStatus.Open => EmbryoStatus.Failed,
                _ => EmbryoStatus.Implanted
            };
            linkedEmbryo.UpdatedAt = DateTime.UtcNow;
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
