using Microsoft.EntityFrameworkCore;
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
        var pregnancyCheckCutoff = today.AddDays(30);
        var dueSoonCutoff = today.AddDays(30);

        var animals = _context.Animals
            .AsNoTracking()
            .ToList();

        var pregChecksDue =
            (from breeding in _context.BreedingEvents.AsNoTracking()
             join animal in _context.Animals.AsNoTracking()
                 on breeding.AnimalId equals animal.AnimalId
             where
                 (
                     breeding.PregnancyStatus == PregnancyStatus.Unconfirmed ||
                     breeding.PregnancyStatus == PregnancyStatus.Recheck
                 )
                 && breeding.PregnancyCheckDueDate.Date <= pregnancyCheckCutoff
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
                     (breeding.PregnancyCheckDueDate.Date - today).Days,

                 IsOverdue =
                     breeding.PregnancyCheckDueDate.Date < today
             })
            .ToList();

        var dueSoon =
            (from breeding in _context.BreedingEvents.AsNoTracking()
             join animal in _context.Animals.AsNoTracking()
                 on breeding.AnimalId equals animal.AnimalId
             where breeding.PregnancyStatus == PregnancyStatus.Pregnant
                   && breeding.ExpectedDueDate.Date >= today
                   && breeding.ExpectedDueDate.Date <= dueSoonCutoff
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
                     (breeding.ExpectedDueDate.Date - today).Days
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

            OverduePregChecksCount =
                pregChecksDue.Count(item => item.IsOverdue),

            UpcomingPregChecksCount =
                pregChecksDue.Count(item => !item.IsOverdue),

            DueSoonCount = dueSoon.Count,

            PregChecksDue = pregChecksDue,
            DueSoon = dueSoon,
            RecentHeats = recentHeats,
            RecentBreedings = recentBreedings
        };
    }
}