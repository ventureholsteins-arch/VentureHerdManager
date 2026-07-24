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

    public object GetDashboard()
    {
        var today = DateTime.Today;
        var pregnancyCheckCutoff = today.AddDays(30);
        var dueSoonCutoff = today.AddDays(30);
        var lutTrackingDays = 4;
        var embryoTrackingDays = 7;

        // Get active animals
        var animals = _context.Animals
            .AsNoTracking()
            .Where(animal => animal.AnimalStatus == AnimalStatus.Active)
            .ToList();

        var milkingCowIds = animals
            .Where(a => a.AnimalStage == AnimalStage.Milking)
            .Select(a => a.AnimalId)
            .ToList();

        // Get latest classifications for milking cows
        var milkingClassifications = _classificationService.GetLatestClassificationsForAnimals(milkingCowIds);
        
        var scoredMilkingScores = milkingClassifications
            .Where(c => c.Score.HasValue)
            .Select(c => c.Score.Value)
            .ToList();

        var scoredMilkingBaas = milkingClassifications
            .Where(c => c.Baa.HasValue)
            .Select(c => c.Baa.Value)
            .ToList();

        // Calculate excellent 2nd lactation+ for milking cows with scores
        var milkingWith2ndLacAndScores = animals
            .Where(a => a.AnimalStage == AnimalStage.Milking && a.CurrentLactation >= 2)
            .Where(a => milkingClassifications.Any(c => c.Score.HasValue))
            .ToList();

        var excellent2ndLacCount = milkingWith2ndLacAndScores.Count > 0
            ? (milkingClassifications.Count(c => c.Score >= 90) * 100m) / milkingWith2ndLacAndScores.Count
            : 0m;

        var pregChecksDue =
            (from breeding in _context.BreedingEvents.AsNoTracking()
             join animal in _context.Animals.AsNoTracking()
                 on breeding.AnimalId equals animal.AnimalId
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

                 AnimalName =
                     animal.BarnName
                     ?? animal.RegisteredName
                     ?? $"Animal {animal.AnimalId}",

                 breeding.SireUsed,
                 breeding.BreedingDate,
                 breeding.PregnancyCheckDueDate,
                 breeding.PregnancyStatus,

                 DaysUntilCheck =
                     (breeding.PregnancyCheckDueDate.Value.Date - today).Days,

                 IsOverdue =
                     breeding.PregnancyCheckDueDate.Value.Date < today
             })
            .ToList();

        var dueSoon =
            (from breeding in _context.BreedingEvents.AsNoTracking()
             join animal in _context.Animals.AsNoTracking()
                 on breeding.AnimalId equals animal.AnimalId
             where breeding.PregnancyStatus == PregnancyStatus.Pregnant
                   && breeding.ExpectedDueDate.HasValue
                   && breeding.ExpectedDueDate.Value.Date >= today
                   && breeding.ExpectedDueDate.Value.Date <= dueSoonCutoff
             orderby breeding.ExpectedDueDate
             select new
             {
                 breeding.BreedingEventId,
                 breeding.AnimalId,

                 AnimalName =
                     animal.BarnName
                     ?? animal.RegisteredName
                     ?? $"Animal {animal.AnimalId}",

                 breeding.SireUsed,
                 breeding.ExpectedDueDate,

                 DaysUntilDue =
                     breeding.ExpectedDueDate.HasValue ? 
                     (breeding.ExpectedDueDate.Value.Date - today).Days : 
                     int.MaxValue
             })
            .ToList();

        var lutTracking =
            (from lut in _context.LutalyseEvents.AsNoTracking()
             join animal in _context.Animals.AsNoTracking()
                 on lut.AnimalId equals animal.AnimalId
             where lut.AdministrationDate.Date >= today.AddDays(-lutTrackingDays)
                   && lut.AdministrationDate.Date <= today
                   && animal.AnimalStatus == AnimalStatus.Active
             orderby lut.AdministrationDate descending
             select new
             {
                 lut.LutalyseEventId,
                 lut.AnimalId,

                 AnimalName =
                     animal.BarnName
                     ?? animal.RegisteredName
                     ?? $"Animal {animal.AnimalId}",

                 lut.AdministrationDate,
                 lut.ExpectedHeatWatchEnd,
                 lut.HeatObserved,

                 DaysTracked =
                     (today - lut.AdministrationDate.Date).Days,

                 DaysRemaining =
                     (lut.ExpectedHeatWatchEnd.Date - today).Days
             })
            .ToList();

        var embryoImplants =
            (from heat in _context.HeatEvents.AsNoTracking()
             join animal in _context.Animals.AsNoTracking()
                 on heat.AnimalId equals animal.AnimalId
             where heat.HasEmbryoTransfer == true
                   && heat.HeatDateTime.Date >= today.AddDays(-embryoTrackingDays)
                   && heat.HeatDateTime.Date <= today
                   && animal.AnimalStatus == AnimalStatus.Active
             orderby heat.HeatDateTime descending
             select new
             {
                 heat.HeatEventId,
                 heat.AnimalId,

                 AnimalName =
                     animal.BarnName
                     ?? animal.RegisteredName
                     ?? $"Animal {animal.AnimalId}",

                 heat.HeatDateTime,
                 heat.EmbryoImplantDate,

                 DaysTracked =
                     (today - heat.HeatDateTime.Date).Days,

                 DaysUntilImplant =
                     (heat.EmbryoImplantDate.HasValue
                         ? (heat.EmbryoImplantDate.Value.Date - today).Days
                         : embryoTrackingDays - (today - heat.HeatDateTime.Date).Days)
             })
            .ToList();

        var recentHeats =
            (from heat in _context.HeatEvents.AsNoTracking()
             join animal in _context.Animals.AsNoTracking()
                 on heat.AnimalId equals animal.AnimalId
             orderby heat.HeatDateTime descending
             select new
             {
                 heat.HeatEventId,
                 heat.AnimalId,

                 AnimalName =
                     animal.BarnName
                     ?? animal.RegisteredName
                     ?? $"Animal {animal.AnimalId}",

                 heat.HeatDateTime,
                 heat.Notes
             })
            .Take(10)
            .ToList();

        var recentBreedings =
            (from breeding in _context.BreedingEvents.AsNoTracking()
             join animal in _context.Animals.AsNoTracking()
                 on breeding.AnimalId equals animal.AnimalId
             where breeding.BreedingDate >= today.AddDays(-45)
                   && breeding.BreedingDate <= today
             orderby breeding.BreedingDate descending
             select new
             {
                 breeding.BreedingEventId,
                 breeding.AnimalId,

                 AnimalName =
                     animal.BarnName
                     ?? animal.RegisteredName
                     ?? $"Animal {animal.AnimalId}",

                 breeding.BreedingDate,
                 breeding.SireUsed,
                 breeding.BreedingType,
                 breeding.PregnancyStatus,
                 breeding.PregnancyCheckDueDate,
                 breeding.ExpectedDueDate
             })
            .Take(10)
            .ToList();

        return new
        {
            TotalAnimals = animals.Count,

            Milking = animals.Count(a => a.AnimalStage == AnimalStage.Milking),

            Dry = animals.Count(a =>
                a.AnimalStage == AnimalStage.Dry),

            Heifers = animals.Count(a =>
                a.AnimalStage == AnimalStage.Heifer),

            Calves = animals.Count(a =>
                a.AnimalStage == AnimalStage.Calf),

            Bulls = animals.Count(a =>
                a.AnimalStage == AnimalStage.Bull),

            PregChecksDueCount = pregChecksDue.Count,

            OverduePregChecksCount =
                pregChecksDue.Count(item => item.IsOverdue),

            UpcomingPregChecksCount =
                pregChecksDue.Count(item => !item.IsOverdue),

            DueSoonCount = dueSoon.Count,

            LutTrackingCount = lutTracking.Count,

            EmbryoImplantsCount = embryoImplants.Count,

            HerdScoreAverage = CalculateHerdAverageExcludingBottom10(scoredMilkingScores),

            HerdBaaAverage = CalculateHerdAverageExcludingBottom10(scoredMilkingBaas),

            AnimalsWithScore = milkingClassifications.Count(c => c.Score.HasValue),

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