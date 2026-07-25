using Microsoft.EntityFrameworkCore;
using VentureHerdManager.Api.Data;
using VentureHerdManager.Api.Models;

namespace VentureHerdManager.Api.Services;

public class DashboardService
{
    private readonly ApplicationDbContext _context;
    private readonly ClassificationService _classificationService;

    public DashboardService(
        ApplicationDbContext context,
        ClassificationService classificationService)
    {
        _context = context;
        _classificationService = classificationService;
    }

    public async Task<object> GetDashboardAsync()
    {
        var today = DateTime.Today;
        var pregnancyCheckCutoff = today.AddDays(30);
        var dueSoonCutoff = today.AddDays(30);
        var lutTrackingDays = 4;
        var embryoTrackingDays = 7;

        var animals = await _context.Animals
            .AsNoTracking()
            .Where(animal => animal.AnimalStatus == AnimalStatus.Active)
            .ToListAsync();

        var animalNameDict = animals.ToDictionary(
            animal => animal.AnimalId,
            animal => animal.BarnName
                ?? animal.RegisteredName
                ?? $"Animal {animal.AnimalId}");

        var milkingCowIds = animals
            .Where(animal => animal.AnimalStage == AnimalStage.Milking)
            .Select(animal => animal.AnimalId)
            .ToList();

        // These queries must run one at a time because they share the same
        // ApplicationDbContext. Running them with Task.WhenAll causes a 500.
        var pregChecksDue = await GetPregChecksDueAsync(
            today,
            pregnancyCheckCutoff,
            animalNameDict);

        var dueSoon = await GetDueSoonAsync(
            today,
            dueSoonCutoff,
            animalNameDict);

        var lutTracking = await GetLutTrackingAsync(
            today,
            lutTrackingDays,
            animalNameDict);

        var embryoImplants = await GetEmbryoImplantsAsync(
            today,
            embryoTrackingDays,
            animalNameDict);

        var recentHeats = await GetRecentHeatsAsync(animalNameDict);

        var recentBreedings = await GetRecentBreedingsAsync(
            today,
            animalNameDict);

        var milkingClassifications =
            await _classificationService
                .GetLatestClassificationsForAnimalsAsync(milkingCowIds);

        var scoredMilkingScores = milkingClassifications
            .Where(classification => classification.Score.HasValue)
            .Select(classification => classification.Score!.Value)
            .ToList();

        var scoredMilkingBaas = milkingClassifications
            .Where(classification => classification.Baa.HasValue)
            .Select(classification => classification.Baa!.Value)
            .ToList();

        var secondLactationOrHigherIds = animals
            .Where(animal =>
                animal.AnimalStage == AnimalStage.Milking
                && animal.CurrentLactation >= 2)
            .Select(animal => animal.AnimalId)
            .ToHashSet();

        var scoredSecondLactationOrHigher =
            milkingClassifications
                .Where(classification =>
                    secondLactationOrHigherIds.Contains(
                        classification.AnimalId)
                    && classification.Score.HasValue)
                .ToList();

        var percentExcellent2ndLactationOrHigher =
            scoredSecondLactationOrHigher.Count > 0
                ? scoredSecondLactationOrHigher.Count(
                      classification => classification.Score >= 90)
                  * 100m
                  / scoredSecondLactationOrHigher.Count
                : 0m;

        return BuildDashboardResponse(
            animals,
            pregChecksDue,
            dueSoon,
            lutTracking,
            embryoImplants,
            recentHeats,
            recentBreedings,
            scoredMilkingScores,
            scoredMilkingBaas,
            percentExcellent2ndLactationOrHigher);
    }

    private async Task<List<dynamic>> GetPregChecksDueAsync(
        DateTime today,
        DateTime pregnancyCheckCutoff,
        Dictionary<int, string> animalNameDict)
    {
        var items = await (
            from breeding in _context.BreedingEvents.AsNoTracking()
            where
                (
                    breeding.PregnancyStatus == PregnancyStatus.Unconfirmed
                    || breeding.PregnancyStatus == PregnancyStatus.Recheck
                )
                && breeding.PregnancyCheckDueDate.HasValue
                && breeding.PregnancyCheckDueDate.Value.Date
                    <= pregnancyCheckCutoff
            orderby breeding.PregnancyCheckDueDate
            select new
            {
                breeding.BreedingEventId,
                breeding.AnimalId,
                breeding.SireUsed,
                breeding.BreedingDate,
                breeding.PregnancyCheckDueDate,
                breeding.PregnancyStatus,
                DaysUntilCheck =
                    (breeding.PregnancyCheckDueDate.Value.Date - today).Days,
                IsOverdue =
                    breeding.PregnancyCheckDueDate.Value.Date < today
            })
            .ToListAsync();

        return items
            .Select(item => new
            {
                item.BreedingEventId,
                item.AnimalId,
                AnimalName = GetAnimalName(
                    item.AnimalId,
                    animalNameDict),
                item.SireUsed,
                item.BreedingDate,
                item.PregnancyCheckDueDate,
                item.PregnancyStatus,
                item.DaysUntilCheck,
                item.IsOverdue
            } as dynamic)
            .ToList();
    }

    private async Task<List<dynamic>> GetDueSoonAsync(
        DateTime today,
        DateTime dueSoonCutoff,
        Dictionary<int, string> animalNameDict)
    {
        var items = await (
            from breeding in _context.BreedingEvents.AsNoTracking()
            where
                breeding.PregnancyStatus == PregnancyStatus.Pregnant
                && breeding.ExpectedDueDate.HasValue
                && breeding.ExpectedDueDate.Value.Date >= today
                && breeding.ExpectedDueDate.Value.Date <= dueSoonCutoff
            orderby breeding.ExpectedDueDate
            select new
            {
                breeding.BreedingEventId,
                breeding.AnimalId,
                breeding.SireUsed,
                breeding.ExpectedDueDate,
                DaysUntilDue =
                    (breeding.ExpectedDueDate!.Value.Date - today).Days
            })
            .ToListAsync();

        return items
            .Select(item => new
            {
                item.BreedingEventId,
                item.AnimalId,
                AnimalName = GetAnimalName(
                    item.AnimalId,
                    animalNameDict),
                item.SireUsed,
                item.ExpectedDueDate,
                item.DaysUntilDue
            } as dynamic)
            .ToList();
    }

    private async Task<List<dynamic>> GetLutTrackingAsync(
        DateTime today,
        int lutTrackingDays,
        Dictionary<int, string> animalNameDict)
    {
        var trackingStart = today.AddDays(-lutTrackingDays);

        var items = await (
            from lut in _context.LutalyseEvents.AsNoTracking()
            where
                lut.AdministrationDate.Date >= trackingStart
                && lut.AdministrationDate.Date <= today
            orderby lut.AdministrationDate descending
            select new
            {
                lut.LutalyseEventId,
                lut.AnimalId,
                lut.AdministrationDate,
                lut.ExpectedHeatWatchEnd,
                lut.HeatObserved,
                DaysTracked =
                    (today - lut.AdministrationDate.Date).Days,
                DaysRemaining =
                    (lut.ExpectedHeatWatchEnd.Date - today).Days
            })
            .ToListAsync();

        return items
            .Select(item => new
            {
                item.LutalyseEventId,
                item.AnimalId,
                AnimalName = GetAnimalName(
                    item.AnimalId,
                    animalNameDict),
                item.AdministrationDate,
                item.ExpectedHeatWatchEnd,
                item.HeatObserved,
                item.DaysTracked,
                item.DaysRemaining
            } as dynamic)
            .ToList();
    }

    private async Task<List<dynamic>> GetEmbryoImplantsAsync(
        DateTime today,
        int embryoTrackingDays,
        Dictionary<int, string> animalNameDict)
    {
        var trackingStart = today.AddDays(-embryoTrackingDays);

        var items = await (
            from heat in _context.HeatEvents.AsNoTracking()
            where
                heat.HasEmbryoTransfer
                && heat.HeatDateTime.Date >= trackingStart
                && heat.HeatDateTime.Date <= today
            orderby heat.HeatDateTime descending
            select new
            {
                heat.HeatEventId,
                heat.AnimalId,
                heat.HeatDateTime,
                heat.EmbryoImplantDate,
                DaysTracked =
                    (today - heat.HeatDateTime.Date).Days,
                DaysUntilImplant =
                    heat.EmbryoImplantDate.HasValue
                        ? (heat.EmbryoImplantDate.Value.Date - today).Days
                        : embryoTrackingDays
                          - (today - heat.HeatDateTime.Date).Days
            })
            .ToListAsync();

        return items
            .Select(item => new
            {
                item.HeatEventId,
                item.AnimalId,
                AnimalName = GetAnimalName(
                    item.AnimalId,
                    animalNameDict),
                item.HeatDateTime,
                item.EmbryoImplantDate,
                item.DaysTracked,
                item.DaysUntilImplant
            } as dynamic)
            .ToList();
    }

    private async Task<List<dynamic>> GetRecentHeatsAsync(
        Dictionary<int, string> animalNameDict)
    {
        var items = await (
            from heat in _context.HeatEvents.AsNoTracking()
            orderby heat.HeatDateTime descending
            select new
            {
                heat.HeatEventId,
                heat.AnimalId,
                heat.HeatDateTime,
                heat.Notes
            })
            .Take(10)
            .ToListAsync();

        return items
            .Select(item => new
            {
                item.HeatEventId,
                item.AnimalId,
                AnimalName = GetAnimalName(
                    item.AnimalId,
                    animalNameDict),
                item.HeatDateTime,
                item.Notes
            } as dynamic)
            .ToList();
    }

    private async Task<List<dynamic>> GetRecentBreedingsAsync(
        DateTime today,
        Dictionary<int, string> animalNameDict)
    {
        var recentStart = today.AddDays(-45);

        var items = await (
            from breeding in _context.BreedingEvents.AsNoTracking()
            where
                breeding.BreedingDate >= recentStart
                && breeding.BreedingDate <= today
            orderby breeding.BreedingDate descending
            select new
            {
                breeding.BreedingEventId,
                breeding.AnimalId,
                breeding.BreedingDate,
                breeding.SireUsed,
                breeding.BreedingType,
                breeding.PregnancyStatus,
                breeding.PregnancyCheckDueDate,
                breeding.ExpectedDueDate
            })
            .Take(10)
            .ToListAsync();

        return items
            .Select(item => new
            {
                item.BreedingEventId,
                item.AnimalId,
                AnimalName = GetAnimalName(
                    item.AnimalId,
                    animalNameDict),
                item.BreedingDate,
                item.SireUsed,
                item.BreedingType,
                item.PregnancyStatus,
                item.PregnancyCheckDueDate,
                item.ExpectedDueDate
            } as dynamic)
            .ToList();
    }

    private object BuildDashboardResponse(
        List<Animal> animals,
        List<dynamic> pregChecksDue,
        List<dynamic> dueSoon,
        List<dynamic> lutTracking,
        List<dynamic> embryoImplants,
        List<dynamic> recentHeats,
        List<dynamic> recentBreedings,
        List<decimal> scoredMilkingScores,
        List<decimal> scoredMilkingBaas,
        decimal percentExcellent2ndLactationOrHigher)
    {
        return new
        {
            TotalAnimals = animals.Count,
            Milking = animals.Count(
                animal => animal.AnimalStage == AnimalStage.Milking),
            Dry = animals.Count(
                animal => animal.AnimalStage == AnimalStage.Dry),
            Heifers = animals.Count(
                animal => animal.AnimalStage == AnimalStage.Heifer),
            Calves = animals.Count(
                animal => animal.AnimalStage == AnimalStage.Calf),
            Bulls = animals.Count(
                animal => animal.AnimalStage == AnimalStage.Bull),

            PregChecksDueCount = pregChecksDue.Count,
            OverduePregChecksCount = pregChecksDue.Count(
                item => item.IsOverdue == true),
            UpcomingPregChecksCount = pregChecksDue.Count(
                item => item.IsOverdue == false),
            DueSoonCount = dueSoon.Count,
            LutTrackingCount = lutTracking.Count,
            EmbryoImplantsCount = embryoImplants.Count,

            HerdScoreAverage =
                CalculateHerdAverageExcludingBottom10(
                    scoredMilkingScores),
            HerdBaaAverage =
                CalculateHerdAverageExcludingBottom10(
                    scoredMilkingBaas),
            AnimalsWithScore = scoredMilkingScores.Count,
            AnimalsWithBaa = scoredMilkingBaas.Count,
            PercentExcellent2ndLactationOrHigher =
                percentExcellent2ndLactationOrHigher,

            PregChecksDue = pregChecksDue,
            DueSoon = dueSoon,
            LutTracking = lutTracking,
            EmbryoImplants = embryoImplants,
            RecentHeats = recentHeats,
            RecentBreedings = recentBreedings
        };
    }

    private static string GetAnimalName(
        int animalId,
        Dictionary<int, string> animalNameDict)
    {
        return animalNameDict.TryGetValue(animalId, out var name)
            ? name
            : $"Animal {animalId}";
    }

    private static decimal? CalculateHerdAverageExcludingBottom10(
        List<decimal> values)
    {
        if (values.Count == 0)
        {
            return null;
        }

        var sorted = values.OrderBy(value => value).ToList();
        var excludeCount = (int)Math.Ceiling(sorted.Count * 0.10);

        if (excludeCount >= sorted.Count)
        {
            return values.Average();
        }

        var filtered = sorted.Skip(excludeCount).ToList();

        return filtered.Count == 0
            ? null
            : filtered.Average();
    }
}