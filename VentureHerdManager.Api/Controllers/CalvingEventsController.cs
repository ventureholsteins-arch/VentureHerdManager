using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VentureHerdManager.Api.Data;
using VentureHerdManager.Api.Models;

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
    public async Task<ActionResult<CalvingEvent>> Create(CalvingEvent calving)
    {
        var animal = await _context.Animals
            .FirstOrDefaultAsync(a => a.AnimalId == calving.AnimalId);

        if (animal == null)
        {
            return NotFound($"Animal {calving.AnimalId} was not found.");
        }

        if (
            !calving.Stillborn &&
            calving.CalfAnimalId == null &&
            (!string.IsNullOrWhiteSpace(calving.CalfBarnName)
             || !string.IsNullOrWhiteSpace(calving.CalfRegisteredName)))
        {
            var calf = new Animal
            {
                BarnName = calving.CalfBarnName,
                RegisteredName = calving.CalfRegisteredName,
                BirthDate = DateOnly.FromDateTime(calving.CalvingDate),
                Sex = calving.CalfSex switch
                {
                    CalfSex.Bull => AnimalSex.Male,
                    CalfSex.Heifer => AnimalSex.Female,
                    _ => AnimalSex.Unknown
                },
                AnimalStage = AnimalStage.Calf,
                AnimalStatus = AnimalStatus.Active,
                DamId = animal.AnimalId,
                DamName = animal.RegisteredName ?? animal.BarnName,
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
            calving);
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