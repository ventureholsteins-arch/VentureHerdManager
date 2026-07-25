using Microsoft.EntityFrameworkCore;
using VentureHerdManager.Api.Data;
using VentureHerdManager.Api.Models;

namespace VentureHerdManager.Api.Services;

public class DashboardService
{
    private readonly ApplicationDbContext _context;
    private readonly ClassificationService _classificationService;

    public DashboardService(ApplicationDbContext context, ClassificationService classificationService)
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

        // Get active animals
        var animals = await _context.Animals
            .AsNoTracking()
            .Where(animal => animal.AnimalStatus == AnimalStatus.Active)
            .ToListAsync();

        var milkingCowIds = animals
            .Where(a => a.AnimalStage == AnimalStage.Milking)
            .Select(a => a.AnimalId)
            .ToList();

        // Run all independent queries in parallel
        var task1 = GetPregChecksDueAsync(today, pregnancyCheckCutoff, animals);
        var task2 = GetDueSoonAsync(today, dueSoonCutoff, animals);
        var task3 = GetLutTrackingAsync(today, lutTrackingDays, animals);
        var task4 = GetEmbryoImplantsAsync(today, embryoTrackingDays, animals);
        var task5 = GetRecentHeatsAsync(animals);
        var task6 = GetRecentBreedingsAsync(today, animals);
        var task7 = _classificationService.GetLatestClassificationsForAnimalsAsync(milkingCowIds);

        await Task.WhenAll(task1, task2, task3, task4, task5, task6, task7);

        var pregChecksDue = task1.Result;
        var dueSoon = task2.Result;
        var lutTracking = task3.Result;
        var embryoImplants = task4.Result;
        var recentHeats = task5.Result;
        var recentBreedings = task6.Result;
        var milkingClassifications = task7.Result;

        var scoredMilkingScores = milkingClassifications
            .Where(c => c.Score.HasValue)
            .Select(c => c.Score.Value)
            .ToList();

        var scoredMilkingBaas = milkingClassifications
            .Where(c => c.Baa.HasValue)
            .Select(c => c.Baa.Value)
            .ToList();

        var milkingWith2ndLacAndScores = animals
            .Where(a => a.AnimalStage == AnimalStage.Milking && a.CurrentLactation >= 2)
            .Where(a => milkingClassifications.Any(c => c.Score.HasValue))
            .ToList();

        var excellent2ndLacCount = milkingWith2ndLacAndScores.Count > 0
            ? (milkingClassifications.Count(c => c.Score >= 90) * 100m) / milkingWith2ndLacAndScores.Count
            : 0m;

        return BuildDashboardResponse(animals, pregChecksDue, dueSoon, lutTracking, embryoImplants, recentHeats, recentBreedings, scoredMilkingScores, scoredMilkingBaas, excellent2ndLacCount);
    }

    private async Task<List<dynamic>> GetPregChecksDueAsync(DateTime today, DateTime pregnancyCheckCutoff, List<Animal> animals)
    {
        return await (from breeding in _context.BreedingEvents.AsNoTracking()
             where
                 (
                     breeding.PregnancyStatus == PregnancyStatus.Unconfirmed ||
                     breeding.PregnancyStatus == PregnancyStatus.Recheck
                 )
                 && breeding.PregnancyCheckDueDate.HasValue
                 && breeding.PregnancyCheckDueDate.Value.Date <= pregnancyCheckCutoff
             orderby breeding.PregnancyCheckDueDate
             select new
             {
                 breeding.BreedingEventId,
                 breeding.AnimalId,
                 AnimalName = animals.Where(a => a.AnimalId == breeding.AnimalId)
                     .Select(a => a.BarnName ?? a.RegisteredName ?? $"Animal {a.AnimalId}")
                     .FirstOrDefault() ?? $"Animal {breeding.AnimalId}",
                 breeding.SireUsed,
                 breeding.BreedingDate,
                 breeding.PregnancyCheckDueDate,
                 breeding.PregnancyStatus,
                 DaysUntilCheck = (breeding.PregnancyCheckDueDate.Value.Date - today).Days,
                 IsOverdue = breeding.PregnancyCheckDueDate.Value.Date < today
             })
            .Cast<dynamic>()
            .ToListAsync();
    }

    private async Task<List<dynamic>> GetDueSoonAsync(DateTime today, DateTime dueSoonCutoff, List<Animal> animals)
    {
        return await (from breeding in _context.BreedingEvents.AsNoTracking()
             where breeding.PregnancyStatus == PregnancyStatus.Pregnant
                   && breeding.ExpectedDueDate.HasValue
                   && breeding.ExpectedDueDate.Value.Date >= today
                   && breeding.ExpectedDueDate.Value.Date <= dueSoonCutoff
             orderby breeding.ExpectedDueDate
             select new
             {
                 breeding.BreedingEventId,
                 breeding.AnimalId,
                 AnimalName = animals.Where(a => a.AnimalId == breeding.AnimalId)
                     .Select(a => a.BarnName ?? a.RegisteredName ?? $"Animal {a.AnimalId}")
                     .FirstOrDefault() ?? $"Animal {breeding.AnimalId}",
                 breeding.SireUsed,
                 breeding.ExpectedDueDate,
                 DaysUntilDue = breeding.ExpectedDueDate.HasValue ? (breeding.ExpectedDueDate.Value.Date - today).Days : int.MaxValue
             })
            .Cast<dynamic>()
            .ToListAsync();
    }

    private async Task<List<dynamic>> GetLutTrackingAsync(DateTime today, int lutTrackingDays, List<Animal> animals)
    {
        return await (from lut in _context.LutalyseEvents.AsNoTracking()
             where lut.AdministrationDate.Date >= today.AddDays(-lutTrackingDays)
                   && lut.AdministrationDate.Date <= today
             orderby lut.AdministrationDate descending
             select new
             {
                 lut.LutalyseEventId,
                 lut.AnimalId,
                 AnimalName = animals.Where(a => a.AnimalId == lut.AnimalId)
                     .Select(a => a.BarnName ?? a.RegisteredName ?? $"Animal {a.AnimalId}")
                     .FirstOrDefault() ?? $"Animal {lut.AnimalId}",
                 lut.AdministrationDate,
                 lut.ExpectedHeatWatchEnd,
                 lut.HeatObserved,
                 DaysTracked = (today - lut.AdministrationDate.Date).Days,
                 DaysRemaining = (lut.ExpectedHeatWatchEnd.Date - today).Days
             })
            .Cast<dynamic>()
            .ToListAsync();
    }

    private async Task<List<dynamic>> GetEmbryoImplantsAsync(DateTime today, int embryoTrackingDays, List<Animal> animals)
    {
        return await (from heat in _context.HeatEvents.AsNoTracking()
             where heat.HasEmbryoTransfer == true
                   && heat.HeatDateTime.Date >= today.AddDays(-embryoTrackingDays)
                   && heat.HeatDateTime.Date <= today
             orderby heat.HeatDateTime descending
             select new
             {
                 heat.HeatEventId,
                 heat.AnimalId,
                 AnimalName = animals.Where(a => a.AnimalId == heat.AnimalId)
                     .Select(a => a.BarnName ?? a.RegisteredName ?? $"Animal {a.AnimalId}")
                     .FirstOrDefault() ?? $"Animal {heat.AnimalId}",
                 heat.HeatDateTime,
                 heat.EmbryoImplantDate,
                 DaysTracked = (today - heat.HeatDateTime.Date).Days,
                 DaysUntilImplant = heat.EmbryoImplantDate.HasValue
                     ? (heat.EmbryoImplantDate.Value.Date - today).Days
                     : embryoTrackingDays - (today - heat.HeatDateTime.Date).Days
             })
            .Cast<dynamic>()
            .ToListAsync();
    }

    private async Task<List<dynamic>> GetRecentHeatsAsync(List<Animal> animals)
    {
        return await (from heat in _context.HeatEvents.AsNoTracking()
             orderby heat.HeatDateTime descending
             select new
             {
                 heat.HeatEventId,
                 heat.AnimalId,
                 AnimalName = animals.Where(a => a.AnimalId == heat.AnimalId)
                     .Select(a => a.BarnName ?? a.RegisteredName ?? $"Animal {a.AnimalId}")
                     .FirstOrDefault() ?? $"Animal {heat.AnimalId}",
                 heat.HeatDateTime,
                 heat.Notes
             })
            .Take(10)
            .Cast<dynamic>()
            .ToListAsync();
    }

    private async Task<List<dynamic>> GetRecentBreedingsAsync(DateTime today, List<Animal> animals)
    {
        return await (from breeding in _context.BreedingEvents.AsNoTracking()
             where breeding.BreedingDate >= today.AddDays(-45)
                   && breeding.BreedingDate <= today
             orderby breeding.BreedingDate descending
             select new
             {
                 breeding.BreedingEventId,
                 breeding.AnimalId,
                 AnimalName = animals.Where(a => a.AnimalId == breeding.AnimalId)
                     .Select(a => a.BarnName ?? a.RegisteredName ?? $"Animal {a.AnimalId}")
                     .FirstOrDefault() ?? $"Animal {breeding.AnimalId}",
                 breeding.BreedingDate,
                 breeding.SireUsed,
                 breeding.BreedingType,
                 breeding.PregnancyStatus,
                 breeding.PregnancyCheckDueDate,
                 breeding.ExpectedDueDate
             })
            .Take(10)
            .Cast<dynamic>()
            .ToListAsync();
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
        decimal excellent2ndLacCount)
    {
        return new
        {
            TotalAnimals = animals.Count,
            Milking = animals.Count(a => a.AnimalStage == AnimalStage.Milking),
            Dry = animals.Count(a => a.AnimalStage == AnimalStage.Dry),
            Heifers = animals.Count(a => a.AnimalStage == AnimalStage.Heifer),
            Calves = animals.Count(a => a.AnimalStage == AnimalStage.Calf),
            Bulls = animals.Count(a => a.AnimalStage == AnimalStage.Bull),
            PregChecksDueCount = pregChecksDue.Count,
            OverduePregChecksCount = pregChecksDue.Where(item => item.IsOverdue == true).Count(),
            UpcomingPregChecksCount = pregChecksDue.Where(item => item.IsOverdue == false).Count(),
            DueSoonCount = dueSoon.Count,
            LutTrackingCount = lutTracking.Count,
            EmbryoImplantsCount = embryoImplants.Count,
            HerdScoreAverage = CalculateHerdAverageExcludingBottom10(scoredMilkingScores),
            HerdBaaAverage = CalculateHerdAverageExcludingBottom10(scoredMilkingBaas),
            AnimalsWithScore = scoredMilkingScores.Count,
            AnimalsWithBaa = scoredMilkingBaas.Count,
            PercentExcellent2ndLactationOrHigher = excellent2ndLacCount,
            PregChecksDue = pregChecksDue,
            DueSoon = dueSoon,
            LutTracking = lutTracking,
            EmbryoImplants = embryoImplants,
            RecentHeats = recentHeats,
            RecentBreedings = recentBreedings
        };
    }

    private decimal? CalculateHerdAverageExcludingBottom10(List<decimal> values)
    {
        if (values.Count == 0)
            return null;

        // Sort in ascending order
        var sorted = values.OrderBy(v => v).ToList();

        // Calculate bottom 10% count (round up)
        var excludeCount = Math.Ceiling(sorted.Count * 0.10);
        var excludeCountInt = (int)excludeCount;

        // If we'd exclude everything, return the average of all
        if (excludeCountInt >= sorted.Count)
            return values.Average();

        // Exclude the bottom 10%
        var filtered = sorted.Skip(excludeCountInt).ToList();

        if (filtered.Count == 0)
            return null;

        return filtered.Average();
    }
}