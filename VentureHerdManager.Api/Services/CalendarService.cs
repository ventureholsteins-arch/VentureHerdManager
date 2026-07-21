using Microsoft.EntityFrameworkCore;
using VentureHerdManager.Api.Data;
using VentureHerdManager.Api.Models;

namespace VentureHerdManager.Api.Services;

public class CalendarService
{
    private readonly ApplicationDbContext _context;

    public CalendarService(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<CalendarEventDto> GetCalendarEvents(
        DateTime startDate,
        DateTime endDate)
    {
        var events = new List<CalendarEventDto>();

        var animals = _context.Animals
            .AsNoTracking()
            .ToDictionary(
                animal => animal.AnimalId,
                animal =>
                    animal.BarnName
                    ?? animal.RegisteredName
                    ?? $"Animal {animal.AnimalId}");

        var heats = _context.HeatEvents
            .AsNoTracking()
            .Where(heat =>
                heat.HeatDateTime >= startDate &&
                heat.HeatDateTime < endDate)
            .ToList();

        foreach (var heat in heats)
        {
            events.Add(new CalendarEventDto
            {
                EventId = $"heat-{heat.HeatEventId}",
                AnimalId = heat.AnimalId,
                AnimalName = GetAnimalName(animals, heat.AnimalId),
                EventType = "heat",
                Title = "Heat",
                EventDate = heat.HeatDateTime,
                Description = heat.Notes
            });
        }

        var breedings = _context.BreedingEvents
            .AsNoTracking()
            .Where(breeding =>
                breeding.BreedingDate >= startDate &&
                breeding.BreedingDate < endDate)
            .ToList();

        foreach (var breeding in breedings)
        {
            events.Add(new CalendarEventDto
            {
                EventId = $"breeding-{breeding.BreedingEventId}",
                AnimalId = breeding.AnimalId,
                AnimalName = GetAnimalName(animals, breeding.AnimalId),
                EventType = "breeding",
                Title = $"Bred to {breeding.SireUsed}",
                EventDate = breeding.BreedingDate,
                Description = breeding.Notes
            });
        }

        var pregnancyChecks = _context.BreedingEvents
            .AsNoTracking()
            .Where(breeding =>
                (
                    breeding.PregnancyStatus ==
                    PregnancyStatus.Unconfirmed ||

                    breeding.PregnancyStatus ==
                    PregnancyStatus.Recheck
                )
                && breeding.PregnancyCheckDueDate >= startDate
                && breeding.PregnancyCheckDueDate < endDate)
            .ToList();

        foreach (var breeding in pregnancyChecks)
        {
            events.Add(new CalendarEventDto
            {
                EventId =
                    $"pregnancy-check-{breeding.BreedingEventId}",

                AnimalId = breeding.AnimalId,
                AnimalName =
                    GetAnimalName(animals, breeding.AnimalId),

                EventType = "pregnancyCheck",
                Title = "Pregnancy check",
                EventDate = breeding.PregnancyCheckDueDate,
                Description = $"Bred to {breeding.SireUsed}"
            });
        }

        var dueDates = _context.BreedingEvents
            .AsNoTracking()
            .Where(breeding =>
                breeding.PregnancyStatus ==
                PregnancyStatus.Pregnant
                && breeding.ExpectedDueDate >= startDate
                && breeding.ExpectedDueDate < endDate)
            .ToList();

        foreach (var breeding in dueDates)
        {
            events.Add(new CalendarEventDto
            {
                EventId = $"due-date-{breeding.BreedingEventId}",
                AnimalId = breeding.AnimalId,
                AnimalName = GetAnimalName(animals, breeding.AnimalId),
                EventType = "dueDate",
                Title = "Expected due date",
                EventDate = breeding.ExpectedDueDate,
                Description = $"Bred to {breeding.SireUsed}"
            });
        }

        var calvings = _context.CalvingEvents
            .AsNoTracking()
            .Where(calving =>
                calving.CalvingDate >= startDate &&
                calving.CalvingDate < endDate)
            .ToList();

        foreach (var calving in calvings)
        {
            events.Add(new CalendarEventDto
            {
                EventId = $"calving-{calving.CalvingEventId}",
                AnimalId = calving.AnimalId,
                AnimalName = GetAnimalName(animals, calving.AnimalId),
                EventType = "calving",
                Title = "Calved",
                EventDate = calving.CalvingDate,
                Description = calving.Notes
            });
        }

        var dryOffs = _context.DryOffEvents
            .AsNoTracking()
            .Where(dryOff =>
                dryOff.DryOffDate >= startDate &&
                dryOff.DryOffDate < endDate)
            .ToList();

        foreach (var dryOff in dryOffs)
        {
            events.Add(new CalendarEventDto
            {
                EventId = $"dry-off-{dryOff.DryOffEventId}",
                AnimalId = dryOff.AnimalId,
                AnimalName = GetAnimalName(animals, dryOff.AnimalId),
                EventType = "dryOff",
                Title = "Dry off",
                EventDate = dryOff.DryOffDate,
                Description =
                    dryOff.Reason ?? dryOff.Notes
            });
        }

        return events
            .OrderBy(calendarEvent => calendarEvent.EventDate)
            .ThenBy(calendarEvent => calendarEvent.AnimalName)
            .ToList();
    }

    private static string GetAnimalName(
        IReadOnlyDictionary<int, string> animals,
        int animalId)
    {
        return animals.TryGetValue(animalId, out var animalName)
            ? animalName
            : $"Animal {animalId}";
    }
}