using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VentureHerdManager.Api.Data;
using VentureHerdManager.Api.Models;
using VentureHerdManager.Api.Services;

namespace VentureHerdManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CalvingEventsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public CalvingEventsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("animal/{animalId}")]
    public async Task<ActionResult<List<CalvingEvent>>> GetByAnimal(int animalId)
    {
        return await _context.CalvingEvents
            .Where(c => c.AnimalId == animalId)
            .OrderByDescending(c => c.CalvingDate)
            .ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<CalvingEvent>> Create(
        [FromBody] CreateCalvingEventRequest request)
    {
        var existingCalving = await _context.CalvingEvents
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate =>
                candidate.AnimalId == request.AnimalId
                && candidate.CalvingDate == request.CalvingDate);
        if (existingCalving != null)
        {
            Response.Headers["X-Duplicate-Prevented"] = "true";
            return Ok(new
            {
                existingCalving.CalvingEventId,
                existingCalving.AnimalId,
                existingCalving.CalfAnimalId
            });
        }

        var calving = new CalvingEvent
        {
            AnimalId = request.AnimalId,
            CalvingDate = request.CalvingDate,
            CalfSex = request.CalfSex,
            CalfBarnName = request.CalfBarnName,
            CalfRegisteredName = request.CalfRegisteredName,
            CalvingEase = request.CalvingEase,
            Twins = request.Twins,
            Stillborn = request.Stillborn,
            BirthWeight = request.BirthWeight,
            Notes = request.Notes,
            PictureUrl = request.PictureUrl,
            CreatedBy = request.CreatedBy
        };

        var animal = await _context.Animals
            .FirstOrDefaultAsync(a => a.AnimalId == calving.AnimalId);

        if (animal == null)
        {
            return NotFound($"Animal {calving.AnimalId} was not found.");
        }

        var completedBreeding = await _context.BreedingEvents
            .Where(breeding =>
                breeding.AnimalId == animal.AnimalId
                && breeding.BreedingDate <= calving.CalvingDate)
            .OrderByDescending(breeding => breeding.BreedingDate)
            .ThenByDescending(breeding => breeding.BreedingEventId)
            .FirstOrDefaultAsync();
        var linkedEmbryo = completedBreeding == null
            ? null
            : await _context.EmbryoRecords.FirstOrDefaultAsync(embryo =>
                embryo.BreedingEventId == completedBreeding.BreedingEventId);

        if (
            !calving.Stillborn &&
            calving.CalfAnimalId == null &&
            (!string.IsNullOrWhiteSpace(calving.CalfBarnName)
             || !string.IsNullOrWhiteSpace(calving.CalfRegisteredName)
             || !string.IsNullOrWhiteSpace(request.CalfSireName)
             || !string.IsNullOrWhiteSpace(request.CalfDamName)))
        {
            var calf = new Animal
            {
                BarnName = calving.CalfBarnName,
                RegisteredName = !string.IsNullOrWhiteSpace(calving.CalfRegisteredName)
                    ? calving.CalfRegisteredName
                    : (!string.IsNullOrWhiteSpace(request.CalfDamName) || !string.IsNullOrWhiteSpace(request.CalfSireName))
                        ? $"{request.CalfDamName ?? animal.RegisteredName ?? animal.BarnName ?? "Dam"} x {request.CalfSireName ?? "Sire not entered"}"
                        : null,
                BirthDate = DateOnly.FromDateTime(calving.CalvingDate),
                Sex = calving.CalfSex switch
                {
                    CalfSex.Bull => AnimalSex.Male,
                    CalfSex.Heifer => AnimalSex.Female,
                    _ => AnimalSex.Unknown
                },
                AnimalStage = AnimalStage.Calf,
                AnimalStatus = AnimalStatus.Active,
                SireName = linkedEmbryo?.Sire ?? request.CalfSireName,
                DamId = linkedEmbryo?.DonorAnimalId ?? animal.AnimalId,
                DamName = linkedEmbryo?.Donor
                    ?? (!string.IsNullOrWhiteSpace(request.CalfDamName)
                        ? request.CalfDamName
                        : animal.RegisteredName ?? animal.BarnName),
                Breed = animal.Breed,
                CreatedBy = calving.CreatedBy,
                UpdatedBy = calving.CreatedBy
            };

            _context.Animals.Add(calf);
            await _context.SaveChangesAsync();
            calving.CalfAnimalId = calf.AnimalId;
        }

        _context.CalvingEvents.Add(calving);

        animal.AnimalStage = AnimalStage.Milking;

        if (completedBreeding != null)
        {
            ReproductiveEventRules.CompleteByCalving(
                completedBreeding,
                calving.CalvingDate);
            if (linkedEmbryo != null)
            {
                ReproductiveEventRules.CompleteEmbryoByCalving(
                    linkedEmbryo,
                    calving.CalvingDate);
            }
        }

        await _context.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(calving.PictureUrl))
        {
            _context.AnimalPhotos.Add(new AnimalPhoto
            {
                AnimalId = calving.AnimalId,
                PhotoUrl = calving.PictureUrl,
                PhotoType = AnimalPhotoType.Calving,
                RelatedEventId = calving.CalvingEventId,
                RelatedEventType = nameof(CalvingEvent),
                Caption = "Calving event photo",
                CreatedBy = calving.CreatedBy
            });

            if (calving.CalfAnimalId is int calfAnimalId)
            {
                _context.AnimalPhotos.Add(new AnimalPhoto
                {
                    AnimalId = calfAnimalId,
                    PhotoUrl = calving.PictureUrl,
                    PhotoType = AnimalPhotoType.Calf,
                    RelatedEventId = calving.CalvingEventId,
                    RelatedEventType = nameof(CalvingEvent),
                    Caption = "Birth photo",
                    CreatedBy = calving.CreatedBy
                });

                var calfAnimal = await _context.Animals
                    .FirstOrDefaultAsync(a => a.AnimalId == calfAnimalId);

                if (calfAnimal != null && string.IsNullOrWhiteSpace(calfAnimal.ProfilePictureUrl))
                {
                    calfAnimal.ProfilePictureUrl = calving.PictureUrl;
                    calfAnimal.UpdatedAt = DateTime.UtcNow;
                    calfAnimal.UpdatedBy = calving.CreatedBy;
                }
            }

            await _context.SaveChangesAsync();
        }

        return CreatedAtAction(
            nameof(GetByAnimal),
            new { animalId = calving.AnimalId },
            new
            {
                calving.CalvingEventId,
                calving.AnimalId,
                calving.CalfAnimalId
            });
    }

    [HttpPost("{calvingEventId:int}/photo")]
    public async Task<IActionResult> AttachPhoto(
        int calvingEventId,
        [FromBody] AttachCalvingPhotoRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PictureUrl))
        {
            return BadRequest("A photo URL is required.");
        }

        var calving = await _context.CalvingEvents
            .FirstOrDefaultAsync(value => value.CalvingEventId == calvingEventId);
        if (calving == null)
        {
            return NotFound();
        }

        calving.PictureUrl = request.PictureUrl.Trim();
        calving.UpdatedAt = DateTime.UtcNow;
        calving.UpdatedBy = request.UpdatedBy;

        if (!await _context.AnimalPhotos.AnyAsync(photo =>
                photo.RelatedEventId == calvingEventId
                && photo.RelatedEventType == nameof(CalvingEvent)
                && photo.PhotoUrl == calving.PictureUrl))
        {
            _context.AnimalPhotos.Add(new AnimalPhoto
            {
                AnimalId = calving.AnimalId,
                PhotoUrl = calving.PictureUrl,
                PhotoType = AnimalPhotoType.Calving,
                RelatedEventId = calvingEventId,
                RelatedEventType = nameof(CalvingEvent),
                Caption = "Calving event photo",
                CreatedBy = request.UpdatedBy
            });
        }

        if (calving.CalfAnimalId is int calfAnimalId)
        {
            var calf = await _context.Animals.FindAsync(calfAnimalId);
            if (calf != null && string.IsNullOrWhiteSpace(calf.ProfilePictureUrl))
            {
                calf.ProfilePictureUrl = calving.PictureUrl;
                calf.UpdatedAt = DateTime.UtcNow;
                calf.UpdatedBy = request.UpdatedBy;
            }
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("{calvingEventId}")]
    public async Task<IActionResult> Update(
        int calvingEventId,
        [FromBody] UpdateCalvingEventRequest request)
    {
        var calving = await _context.CalvingEvents
            .FirstOrDefaultAsync(c => c.CalvingEventId == calvingEventId);

        if (calving == null)
        {
            return NotFound();
        }

        calving.CalvingDate = request.CalvingDate;
        calving.CalfSex = request.CalfSex;
        calving.CalfBarnName = request.CalfBarnName;
        calving.CalfRegisteredName = request.CalfRegisteredName;
        calving.CalvingEase = request.CalvingEase;
        calving.Twins = request.Twins;
        calving.Stillborn = request.Stillborn;
        calving.Notes = request.Notes;
        calving.PictureUrl = request.PictureUrl;
        calving.UpdatedBy = request.UpdatedBy;
        calving.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{calvingEventId}")]
    public async Task<IActionResult> Delete(int calvingEventId)
    {
        var calving = await _context.CalvingEvents
            .FirstOrDefaultAsync(c => c.CalvingEventId == calvingEventId);

        if (calving == null)
        {
            return NotFound();
        }

        _context.CalvingEvents.Remove(calving);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

public sealed class AttachCalvingPhotoRequest
{
    public string PictureUrl { get; set; } = string.Empty;
    public string? UpdatedBy { get; set; }
}

public class CreateCalvingEventRequest
{
    public int AnimalId { get; set; }

    public DateTime CalvingDate { get; set; } = DateTime.UtcNow;

    public CalfSex CalfSex { get; set; } = CalfSex.Unknown;

    public string? CalfBarnName { get; set; }

    public string? CalfRegisteredName { get; set; }

    public string? CalfSireName { get; set; }

    public string? CalfDamName { get; set; }

    public CalvingEase CalvingEase { get; set; } = CalvingEase.Unassisted;

    public bool Twins { get; set; }

    public bool Stillborn { get; set; }

    public decimal? BirthWeight { get; set; }

    public string? PictureUrl { get; set; }

    public string? Notes { get; set; }

    public string? CreatedBy { get; set; }
}

public class UpdateCalvingEventRequest
{
    public DateTime CalvingDate { get; set; }

    public CalfSex CalfSex { get; set; }

    public string? CalfBarnName { get; set; }

    public string? CalfRegisteredName { get; set; }

    public CalvingEase CalvingEase { get; set; }

    public bool Twins { get; set; }

    public bool Stillborn { get; set; }

    public string? Notes { get; set; }

    public string? PictureUrl { get; set; }

    public string? UpdatedBy { get; set; }
}
