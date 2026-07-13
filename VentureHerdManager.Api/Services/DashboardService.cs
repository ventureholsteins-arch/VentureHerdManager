using VentureHerdManager.Api.Data;
using VentureHerdManager.Api.Models;

namespace VentureHerdManager.Api.Services;

public class DashboardService
{
    private readonly ApplicationDbContext _context;

    public DashboardService(ApplicationDbContext context)
    {
        _context = context;
    }

    public object GetDashboard()
    {
        var today = DateTime.Today;
        var dueSoonCutoff = today.AddDays(30);

        var animals = _context.Animals.ToList();

        var pregChecksDue =
            (from breeding in _context.BreedingEvents
             join animal in _context.Animals
                 on breeding.AnimalId equals animal.AnimalId
             where breeding.PregnancyStatus == PregnancyStatus.Unconfirmed
                   && breeding.PregnancyCheckDueDate.Date <= today
             orderby breeding.PregnancyCheckDueDate
             select new
             {
                 breeding.BreedingEventId,
                 breeding.AnimalId,
                 AnimalName = animal.BarnName
                              ?? animal.RegisteredName
                              ?? $"Animal {animal.AnimalId}",
                 breeding.SireUsed,
                 breeding.BreedingDate,
                 breeding.PregnancyCheckDueDate,
                 breeding.PregnancyStatus
             })
            .ToList();

        var dueSoon =
            (from breeding in _context.BreedingEvents
             join animal in _context.Animals
                 on breeding.AnimalId equals animal.AnimalId
             where breeding.PregnancyStatus == PregnancyStatus.Pregnant
                   && breeding.ExpectedDueDate.Date >= today
                   && breeding.ExpectedDueDate.Date <= dueSoonCutoff
             orderby breeding.ExpectedDueDate
             select new
             {
                 breeding.BreedingEventId,
                 breeding.AnimalId,
                 AnimalName = animal.BarnName
                              ?? animal.RegisteredName
                              ?? $"Animal {animal.AnimalId}",
                 breeding.SireUsed,
                 breeding.ExpectedDueDate,
                 DaysUntilDue =
                     (breeding.ExpectedDueDate.Date - today).Days
             })
            .ToList();

        var recentHeats =
            (from heat in _context.HeatEvents
             join animal in _context.Animals
                 on heat.AnimalId equals animal.AnimalId
             orderby heat.HeatDateTime descending
             select new
             {
                 heat.HeatEventId,
                 heat.AnimalId,
                 AnimalName = animal.BarnName
                              ?? animal.RegisteredName
                              ?? $"Animal {animal.AnimalId}",
                 heat.HeatDateTime,
                 heat.Notes
             })
            .Take(10)
            .ToList();

        var recentBreedings =
            (from breeding in _context.BreedingEvents
             join animal in _context.Animals
                 on breeding.AnimalId equals animal.AnimalId
             orderby breeding.BreedingDate descending
             select new
             {
                 breeding.BreedingEventId,
                 breeding.AnimalId,
                 AnimalName = animal.BarnName
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

            Milking = animals.Count(a =>
                a.AnimalStage == AnimalStage.Milking),

            Dry = animals.Count(a =>
                a.AnimalStage == AnimalStage.Dry),

            Heifers = animals.Count(a =>
                a.AnimalStage == AnimalStage.Heifer),

            Calves = animals.Count(a =>
                a.AnimalStage == AnimalStage.Calf),

            Bulls = animals.Count(a =>
                a.AnimalStage == AnimalStage.Bull),

            PregChecksDueCount = pregChecksDue.Count,
            DueSoonCount = dueSoon.Count,

            PregChecksDue = pregChecksDue,
            DueSoon = dueSoon,
            RecentHeats = recentHeats,
            RecentBreedings = recentBreedings
        };
    }
}