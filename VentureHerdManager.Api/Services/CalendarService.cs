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
        if (endDate <= startDate)
        {
            throw new ArgumentException(
                "The calendar end date must be after the start date.");
        }

        var events = new List<CalendarEventDto>();

        var activeAnimals = _context.Animals
            .AsNoTracking()
            .Where(animal =>
                animal.AnimalStatus == AnimalStatus.Active)
            .Select(animal => new
            {
                animal.AnimalId,

                AnimalName =
                    animal.BarnName
                    ?? animal.RegisteredName
                    ?? $"Animal {animal.AnimalId}"
            })
            .ToDictionary(
                animal => animal.AnimalId,
                animal => animal.AnimalName);

        AddHeatEvents(
            events,
            activeAnimals,
            startDate,
            endDate);

        AddBreedingEvents(
            events,
            activeAnimals,
            startDate,
            endDate);

        AddPregnancyCheckEvents(
            events,
            activeAnimals,
            startDate,
            endDate);

        AddExpectedDueDateEvents(
            events,
            activeAnimals,
            startDate,
            endDate);

        AddCalvingEvents(
            events,
            activeAnimals,
            startDate,
            endDate);

        AddDryOffEvents(
            events,
            activeAnimals,
            startDate,
            endDate);

        AddLutalyseEvents(
            events,
            activeAnimals,
            startDate,
            endDate);

        AddClassificationEvents(
            events,
            activeAnimals,
            startDate,
            endDate);

        return events
            .OrderBy(calendarEvent =>
                calendarEvent.EventDate)
            .ThenBy(calendarEvent =>
                calendarEvent.AnimalName)
            .ThenBy(calendarEvent =>
                calendarEvent.Title)
            .ToList();
    }

    private void AddHeatEvents(
        ICollection<CalendarEventDto> events,
        IReadOnlyDictionary<int, string> animals,
        DateTime startDate,
        DateTime endDate)
    {
        var heatEvents = _context.HeatEvents
            .AsNoTracking()
            .Where(heat =>
                heat.HeatDateTime >= startDate &&
                heat.HeatDateTime < endDate)
            .ToList();

        foreach (var heat in heatEvents)
        {
            if (!animals.ContainsKey(heat.AnimalId))
            {
                continue;
            }

            events.Add(new CalendarEventDto
            {
                EventId =
                    $"heat-{heat.HeatEventId}",

                AnimalId =
                    heat.AnimalId,

                AnimalName =
                    GetAnimalName(
                        animals,
                        heat.AnimalId),

                EventType =
                    "heat",

                Title =
                    "Heat",

                EventDate =
                    heat.HeatDateTime,

                Description =
                    BuildDescription(
                        heat.Notes,
                        heat.PictureUrl == null
                            ? null
                            : "Photo attached")
            });
        }
    }

    private void AddBreedingEvents(
        ICollection<CalendarEventDto> events,
        IReadOnlyDictionary<int, string> animals,
        DateTime startDate,
        DateTime endDate)
    {
        var breedingEvents = _context.BreedingEvents
            .AsNoTracking()
            .Where(breeding =>
                breeding.BreedingDate >= startDate &&
                breeding.BreedingDate < endDate)
            .ToList();

        foreach (var breeding in breedingEvents)
        {
            if (!animals.ContainsKey(breeding.AnimalId))
            {
                continue;
            }

            var sireDescription =
                string.IsNullOrWhiteSpace(
                    breeding.SireUsed)
                    ? null
                    : $"Sire: {breeding.SireUsed}";

            events.Add(new CalendarEventDto
            {
                EventId =
                    $"breeding-{breeding.BreedingEventId}",

                AnimalId =
                    breeding.AnimalId,

                AnimalName =
                    GetAnimalName(
                        animals,
                        breeding.AnimalId),

                EventType =
                    "breeding",

                Title =
                    string.IsNullOrWhiteSpace(
                        breeding.SireUsed)
                        ? "Breeding"
                        : $"Bred to {breeding.SireUsed}",

                EventDate =
                    breeding.BreedingDate,

                Description =
                    BuildDescription(
                        sireDescription,
                        $"Type: {FormatBreedingType(
                            breeding.BreedingType)}",
                        breeding.Notes)
            });
        }
    }

    private void AddPregnancyCheckEvents(
        ICollection<CalendarEventDto> events,
        IReadOnlyDictionary<int, string> animals,
        DateTime startDate,
        DateTime endDate)
    {
        var pregnancyChecks =
            _context.BreedingEvents
                .AsNoTracking()
                .Where(breeding =>
                    (
                        breeding.PregnancyStatus ==
                            PregnancyStatus.Unconfirmed
                        ||
                        breeding.PregnancyStatus ==
                            PregnancyStatus.Recheck
                    )
                    &&
                    breeding.PregnancyCheckDueDate.HasValue
                    &&
                    breeding.PregnancyCheckDueDate
                        >= startDate
                    &&
                    breeding.PregnancyCheckDueDate
                        < endDate)
                .ToList();

        foreach (var breeding in pregnancyChecks)
        {
            if (!animals.ContainsKey(breeding.AnimalId) || !breeding.PregnancyCheckDueDate.HasValue)
            {
                continue;
            }

            var statusText =
                breeding.PregnancyStatus ==
                    PregnancyStatus.Recheck
                    ? "Recheck needed"
                    : "Initial pregnancy check";

            events.Add(new CalendarEventDto
            {
                EventId =
                    $"pregnancy-check-" +
                    $"{breeding.BreedingEventId}",

                AnimalId =
                    breeding.AnimalId,

                AnimalName =
                    GetAnimalName(
                        animals,
                        breeding.AnimalId),

                EventType =
                    "pregnancyCheck",

                Title =
                    breeding.PregnancyStatus ==
                        PregnancyStatus.Recheck
                        ? "Pregnancy recheck"
                        : "Pregnancy check",

                EventDate =
                    breeding.PregnancyCheckDueDate.Value,

                Description =
                    BuildDescription(
                        statusText,
                        string.IsNullOrWhiteSpace(
                            breeding.SireUsed)
                            ? null
                            : $"Bred to {breeding.SireUsed}",
                        breeding.Notes)
            });
        }
    }

    private void AddExpectedDueDateEvents(
        ICollection<CalendarEventDto> events,
        IReadOnlyDictionary<int, string> animals,
        DateTime startDate,
        DateTime endDate)
    {
        var expectedDueDates =
            _context.BreedingEvents
                .AsNoTracking()
                .Where(breeding =>
                    breeding.PregnancyStatus ==
                        PregnancyStatus.Pregnant
                    &&
                    breeding.ExpectedDueDate.HasValue
                    &&
                    breeding.ExpectedDueDate >= startDate
                    &&
                    breeding.ExpectedDueDate < endDate)
                .ToList();

        foreach (var breeding in expectedDueDates)
        {
            if (!animals.ContainsKey(breeding.AnimalId) || !breeding.ExpectedDueDate.HasValue)
            {
                continue;
            }

            events.Add(new CalendarEventDto
            {
                EventId =
                    $"due-date-{breeding.BreedingEventId}",

                AnimalId =
                    breeding.AnimalId,

                AnimalName =
                    GetAnimalName(
                        animals,
                        breeding.AnimalId),

                EventType =
                    "dueDate",

                Title =
                    "Expected calving",

                EventDate =
                    breeding.ExpectedDueDate.Value,

                Description =
                    BuildDescription(
                        string.IsNullOrWhiteSpace(
                            breeding.SireUsed)
                            ? null
                            : $"Bred to {breeding.SireUsed}",
                        $"Breeding type: " +
                        FormatBreedingType(
                            breeding.BreedingType),
                        breeding.Notes)
            });
        }
    }

    private void AddCalvingEvents(
        ICollection<CalendarEventDto> events,
        IReadOnlyDictionary<int, string> animals,
        DateTime startDate,
        DateTime endDate)
    {
        var calvingEvents =
            _context.CalvingEvents
                .AsNoTracking()
                .Where(calving =>
                    calving.CalvingDate >= startDate &&
                    calving.CalvingDate < endDate)
                .ToList();

        foreach (var calving in calvingEvents)
        {
            if (!animals.ContainsKey(calving.AnimalId))
            {
                continue;
            }

            var calfDescription =
                calving.CalfSex switch
                {
                    CalfSex.Heifer => "Heifer calf",
                    CalfSex.Bull => "Bull calf",
                    _ => "Calf sex unknown"
                };

            if (!string.IsNullOrWhiteSpace(
                    calving.CalfBarnName))
            {
                calfDescription +=
                    $" — {calving.CalfBarnName}";
            }

            events.Add(new CalendarEventDto
            {
                EventId =
                    $"calving-{calving.CalvingEventId}",

                AnimalId =
                    calving.AnimalId,

                AnimalName =
                    GetAnimalName(
                        animals,
                        calving.AnimalId),

                EventType =
                    "calving",

                Title =
                    calving.Twins
                        ? "Calved twins"
                        : "Calved",

                EventDate =
                    calving.CalvingDate,

                Description =
                    BuildDescription(
                        calfDescription,
                        calving.Stillborn
                            ? "Stillborn"
                            : null,
                        calving.PictureUrl == null
                            ? null
                            : "Photo attached",
                        calving.Notes)
            });
        }
    }

    private void AddDryOffEvents(
        ICollection<CalendarEventDto> events,
        IReadOnlyDictionary<int, string> animals,
        DateTime startDate,
        DateTime endDate)
    {
        var dryOffEvents =
            _context.DryOffEvents
                .AsNoTracking()
                .Where(dryOff =>
                    dryOff.DryOffDate >= startDate &&
                    dryOff.DryOffDate < endDate)
                .ToList();

        foreach (var dryOff in dryOffEvents)
        {
            if (!animals.ContainsKey(dryOff.AnimalId))
            {
                continue;
            }

            events.Add(new CalendarEventDto
            {
                EventId =
                    $"dry-off-{dryOff.DryOffEventId}",

                AnimalId =
                    dryOff.AnimalId,

                AnimalName =
                    GetAnimalName(
                        animals,
                        dryOff.AnimalId),

                EventType =
                    "dryOff",

                Title =
                    "Dry off",

                EventDate =
                    dryOff.DryOffDate,

                Description =
                    BuildDescription(
                        dryOff.Reason,
                        dryOff.Notes)
            });
        }
    }

    private void AddLutalyseEvents(
        ICollection<CalendarEventDto> events,
        IReadOnlyDictionary<int, string> animals,
        DateTime startDate,
        DateTime endDate)
    {
        var administrations =
            _context.LutalyseEvents
                .AsNoTracking()
                .Where(lutalyse =>
                    lutalyse.AdministrationDate >= startDate &&
                    lutalyse.AdministrationDate < endDate)
                .ToList();

        foreach (var lutalyse in administrations)
        {
            if (!animals.ContainsKey(lutalyse.AnimalId))
            {
                continue;
            }

            events.Add(new CalendarEventDto
            {
                EventId =
                    $"lutalyse-{lutalyse.LutalyseEventId}",

                AnimalId =
                    lutalyse.AnimalId,

                AnimalName =
                    GetAnimalName(
                        animals,
                        lutalyse.AnimalId),

                EventType =
                    "lutalyse",

                Title =
                    "Lutalyse administered",

                EventDate =
                    lutalyse.AdministrationDate,

                Description =
                    BuildDescription(
                        $"Watch from " +
                        $"{lutalyse.ExpectedHeatWatchStart:d} " +
                        $"through " +
                        $"{lutalyse.ExpectedHeatWatchEnd:d}",
                        lutalyse.Notes)
            });
        }

        var watchWindows =
            _context.LutalyseEvents
                .AsNoTracking()
                .Where(lutalyse =>
                    !lutalyse.HeatObserved
                    &&
                    lutalyse.ExpectedHeatWatchStart < endDate
                    &&
                    lutalyse.ExpectedHeatWatchEnd >= startDate)
                .ToList();

        foreach (var lutalyse in watchWindows)
        {
            if (!animals.ContainsKey(lutalyse.AnimalId))
            {
                continue;
            }

            var displayedWatchDate =
                lutalyse.ExpectedHeatWatchStart < startDate
                    ? startDate
                    : lutalyse.ExpectedHeatWatchStart;

            events.Add(new CalendarEventDto
            {
                EventId =
                    $"lutalyse-watch-" +
                    $"{lutalyse.LutalyseEventId}",

                AnimalId =
                    lutalyse.AnimalId,

                AnimalName =
                    GetAnimalName(
                        animals,
                        lutalyse.AnimalId),

                EventType =
                    "lutalyseWatch",

                Title =
                    "Lutalyse heat watch",

                EventDate =
                    displayedWatchDate,

                Description =
                    BuildDescription(
                        $"Watch window ends " +
                        $"{lutalyse.ExpectedHeatWatchEnd:d}",
                        lutalyse.Notes)
            });
        }
    }

    private void AddClassificationEvents(
        ICollection<CalendarEventDto> events,
        IReadOnlyDictionary<int, string> animals,
        DateTime startDate,
        DateTime endDate)
    {
        var classifications =
            _context.ClassificationRecords
                .AsNoTracking()
                .Where(record =>
                    record.ClassificationDate >= startDate &&
                    record.ClassificationDate < endDate)
                .ToList();

        foreach (var classification in classifications)
        {
            if (!animals.ContainsKey(
                    classification.AnimalId))
            {
                continue;
            }

            events.Add(new CalendarEventDto
            {
                EventId =
                    $"classification-" +
                    $"{classification.ClassificationRecordId}",

                AnimalId =
                    classification.AnimalId,

                AnimalName =
                    GetAnimalName(
                        animals,
                        classification.AnimalId),

                EventType =
                    "classification",

                Title =
                    string.IsNullOrWhiteSpace(
                        classification.ClassificationLabel)
                        ? $"Classified {classification.Score:0.#}"
                        : classification.ClassificationLabel,

                EventDate =
                    classification.ClassificationDate ?? DateTime.UtcNow,

                Description =
                    BuildDescription(
                        $"Score: {classification.Score:0.##}",
                        classification.Baa.HasValue
                            ? $"BAA: " +
                              $"{classification.Baa.Value:0.##}"
                            : null,
                        classification.AgeInMonthsAtScoring.HasValue
                            ? FormatAgeAtScoring(
                                classification
                                    .AgeInMonthsAtScoring.Value)
                            : null,
                        classification.Notes)
            });
        }
    }

    private static string GetAnimalName(
        IReadOnlyDictionary<int, string> animals,
        int animalId)
    {
        return animals.TryGetValue(
            animalId,
            out var animalName)
            ? animalName
            : $"Animal {animalId}";
    }

    private static string FormatBreedingType(
        BreedingType breedingType)
    {
        return breedingType switch
        {
            BreedingType.AI => "AI",
            BreedingType.Natural => "Natural",
            BreedingType.EmbryoTransfer =>
                "Embryo transfer",
            _ => breedingType.ToString()
        };
    }

    private static string FormatAgeAtScoring(
        int ageInMonths)
    {
        var years = ageInMonths / 12;
        var months = ageInMonths % 12;

        if (years <= 0)
        {
            return months == 1
                ? "Age scored: 1 month"
                : $"Age scored: {months} months";
        }

        if (months == 0)
        {
            return years == 1
                ? "Age scored: 1 yr old"
                : $"Age scored: {years} yr old";
        }

        return $"Age scored: {years} yr {months} mo";
    }

    private static string? BuildDescription(
        params string?[] values)
    {
        var meaningfulValues = values
            .Where(value =>
                !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim())
            .ToList();

        return meaningfulValues.Count == 0
            ? null
            : string.Join(" • ", meaningfulValues);
    }
}