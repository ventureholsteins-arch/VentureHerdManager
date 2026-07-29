using Microsoft.EntityFrameworkCore;
using VentureHerdManager.Api.Data;
using VentureHerdManager.Api.DTOs;
using VentureHerdManager.Api.Models;

namespace VentureHerdManager.Api.Services;

public class AnimalSnapshotService
{
    private readonly ApplicationDbContext _context;

    public AnimalSnapshotService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AnimalSnapshotDto> GetSnapshotAsync(int animalId, CancellationToken cancellationToken = default)
    {
        var animal = await _context.Animals
            .AsNoTracking()
            .Include(animal => animal.Dam)
            .Include(animal => animal.Sire)
            .FirstOrDefaultAsync(animal => animal.AnimalId == animalId, cancellationToken);

        if (animal == null)
        {
            throw new KeyNotFoundException($"Animal {animalId} was not found.");
        }

        // Safely load all related events with error handling
        var heatEvents = new List<HeatEvent>();
        var breedingEvents = new List<BreedingEvent>();
        var calvingEvents = new List<CalvingEvent>();
            var dryOffEvents = new List<DryOffEvent>();
            var classificationRecords = new List<ClassificationRecord>();
            var notes = new List<AnimalNote>();
            var photos = new List<AnimalPhoto>();

            try
            {
                heatEvents = await _context.HeatEvents
                    .AsNoTracking()
                    .Where(heat => heat.AnimalId == animalId)
                    .OrderByDescending(heat => heat.HeatDateTime)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex) { Console.WriteLine($"Heat events error: {ex.Message}"); }

            try
            {
                breedingEvents = await _context.BreedingEvents
                    .AsNoTracking()
                    .Where(breeding => breeding.AnimalId == animalId)
                    .OrderByDescending(breeding => breeding.BreedingDate)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex) { Console.WriteLine($"Breeding events error: {ex.Message}"); }

            try
            {
                calvingEvents = await _context.CalvingEvents
                    .AsNoTracking()
                    .Where(calving => calving.AnimalId == animalId)
                    .OrderByDescending(calving => calving.CalvingDate)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex) { Console.WriteLine($"Calving events error: {ex.Message}"); }

            try
            {
                dryOffEvents = await _context.DryOffEvents
                    .AsNoTracking()
                    .Where(dryOff => dryOff.AnimalId == animalId)
                    .OrderByDescending(dryOff => dryOff.DryOffDate)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex) { Console.WriteLine($"Dry off events error: {ex.Message}"); }

            try
            {
                classificationRecords = await _context.ClassificationRecords
                    .AsNoTracking()
                    .Where(record => record.AnimalId == animalId)
                    .OrderByDescending(record => record.ClassificationDate)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex) { Console.WriteLine($"Classification records error: {ex.Message}"); }

            try
            {
                notes = await _context.AnimalNotes
                    .AsNoTracking()
                    .Where(note => note.AnimalId == animalId)
                    .OrderByDescending(note => note.NoteDate)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex) { Console.WriteLine($"Animal notes error: {ex.Message}"); }

            try
            {
                photos = await _context.AnimalPhotos
                    .AsNoTracking()
                    .Where(photo => photo.AnimalId == animalId)
                    .OrderByDescending(photo => photo.PhotoDate)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex) { Console.WriteLine($"Animal photos error: {ex.Message}"); }

        var timeline = new List<AnimalTimelineEntryDto>();

        timeline.AddRange(heatEvents.Select(heat => new AnimalTimelineEntryDto
        {
            EventId = heat.HeatEventId,
            EventType = "Heat",
            Title = "Heat",
            Summary = GetHeatSummary(heat),
            EventDate = heat.HeatDateTime,
            Notes = heat.Notes,
            PhotoUrl = heat.PictureUrl
        }));

        timeline.AddRange(breedingEvents.Select(breeding => new AnimalTimelineEntryDto
        {
            EventId = breeding.BreedingEventId,
            EventType = "Breeding",
            Title = "Breeding",
            Summary = GetBreedingSummary(breeding),
            EventDate = breeding.BreedingDate,
            Notes = breeding.Notes,
            PhotoUrl = null
        }));

        timeline.AddRange(calvingEvents.Select(calving => new AnimalTimelineEntryDto
        {
            EventId = calving.CalvingEventId,
            EventType = "Calving",
            Title = "Calving",
            Summary = GetCalvingSummary(calving),
            EventDate = calving.CalvingDate,
            Notes = calving.Notes,
            PhotoUrl = calving.PictureUrl
        }));

        timeline.AddRange(dryOffEvents.Select(dryOff => new AnimalTimelineEntryDto
        {
            EventId = dryOff.DryOffEventId,
            EventType = "DryOff",
            Title = "Dry off",
            Summary = GetDryOffSummary(dryOff),
            EventDate = dryOff.DryOffDate,
            Notes = dryOff.Notes,
            PhotoUrl = null
        }));

        timeline.AddRange(classificationRecords
            .Where(record => record.ClassificationDate.HasValue)
            .Select(record => new AnimalTimelineEntryDto
            {
                EventId = record.ClassificationRecordId,
                EventType = "Classification",
                Title = "Classification",
                Summary = GetClassificationSummary(record),
                EventDate = record.ClassificationDate.Value,
                Notes = record.Notes,
                PhotoUrl = null
            }));

        timeline.AddRange(notes.Select(note => new AnimalTimelineEntryDto
        {
            EventId = note.AnimalNoteId,
            EventType = "Note",
            Title = "Note",
            Summary = note.NoteText,
            EventDate = note.NoteDate,
            Notes = note.NoteText,
            PhotoUrl = null
        }));

        timeline.AddRange(photos.Select(photo => new AnimalTimelineEntryDto
        {
            EventId = photo.AnimalPhotoId,
            EventType = "Photo",
            Title = "Photo",
            Summary = photo.Caption ?? photo.PhotoType.ToString(),
            EventDate = photo.PhotoDate,
            Notes = photo.Caption,
            PhotoUrl = photo.PhotoUrl
        }));

        timeline = timeline
            .OrderByDescending(entry => entry.EventDate)
            .ToList();

        return new AnimalSnapshotDto
        {
            Animal = animal,
            LatestHeatEvent = heatEvents.FirstOrDefault(),
            LatestBreedingEvent = breedingEvents.FirstOrDefault(),
            LatestCalvingEvent = calvingEvents.FirstOrDefault(),
            LatestDryOffEvent = dryOffEvents.FirstOrDefault(),
            LatestClassificationRecord = classificationRecords.FirstOrDefault(),
            Photos = photos,
            Timeline = timeline
        };
    }

    private static string GetHeatSummary(HeatEvent heat)
    {
        var standingText = heat.StandingHeat ? "standing heat" : "heat observed";
        var strengthText = heat.HeatStrength switch
        {
            HeatStrength.Weak => "weak",
            HeatStrength.Normal => "normal",
            HeatStrength.Strong => "strong",
            _ => "unknown"
        };

        return $"{standingText} ({strengthText})";
    }

    private static string GetBreedingSummary(BreedingEvent breeding)
    {
        var breedingType = breeding.BreedingType switch
        {
            BreedingType.AI => "AI",
            BreedingType.Natural => "Natural",
            BreedingType.EmbryoTransfer => "Embryo transfer",
            _ => breeding.BreedingType.ToString()
        };

        return $"Bred {breedingType} to {breeding.SireUsed}";
    }

    private static string GetCalvingSummary(CalvingEvent calving)
    {
        var calfText = string.IsNullOrWhiteSpace(calving.CalfBarnName)
            ? "Calf recorded"
            : $"Calf {calving.CalfBarnName}";

        return calving.Twins
            ? $"{calfText} (twins)"
            : calfText;
    }

    private static string GetDryOffSummary(DryOffEvent dryOff)
    {
        return string.IsNullOrWhiteSpace(dryOff.Reason)
            ? "Dry off recorded"
            : $"Dry off: {dryOff.Reason}";
    }

    private static string GetClassificationSummary(ClassificationRecord record)
    {
        if (!string.IsNullOrWhiteSpace(record.ClassificationLabel))
        {
            return $"Classified {record.ClassificationLabel}";
        }

        return $"Classified {record.Score:0.##}";
    }
}
