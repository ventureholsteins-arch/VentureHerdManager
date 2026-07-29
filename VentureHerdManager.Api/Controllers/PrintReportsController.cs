using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VentureHerdManager.Api.Data;
using VentureHerdManager.Api.Models;

namespace VentureHerdManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PrintReportsController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var today = DateTime.UtcNow.Date;
        var monthAgo = today.AddMonths(-1);
        var eightMonths = today.AddMonths(8);

        var animals = await context.Animals.AsNoTracking()
            .Where(a => a.AnimalStatus == AnimalStatus.Active)
            .OrderBy(a => a.AnimalStage).ThenBy(a => a.BarnName)
            .Select(a => new {
                a.AnimalId, a.BarnName, a.RegisteredName, a.RegistrationNumber,
                a.BirthDate, a.AnimalStage, a.Breed, a.SireName, a.DamName
            }).ToListAsync();

        var heats = await context.HeatEvents.AsNoTracking()
            .Where(h => h.HeatDateTime >= monthAgo)
            .OrderByDescending(h => h.HeatDateTime)
            .Select(h => new {
                h.HeatEventId, h.AnimalId,
                AnimalName = h.Animal!.BarnName ?? h.Animal.RegisteredName ?? $"Animal #{h.AnimalId}",
                h.HeatDateTime, h.Notes
            }).ToListAsync();

        var breedings = await context.BreedingEvents.AsNoTracking()
            .OrderByDescending(b => b.BreedingDate)
            .Select(b => new {
                b.BreedingEventId, b.AnimalId,
                AnimalName = b.Animal!.BarnName ?? b.Animal.RegisteredName ?? $"Animal #{b.AnimalId}",
                AnimalStage = b.Animal.AnimalStage,
                b.BreedingDate, b.SireUsed, b.BreedingType, b.PregnancyStatus,
                b.ExpectedDueDate, b.PregnancyCheckDueDate
            }).ToListAsync();

        var embryos = await context.EmbryoRecords.AsNoTracking()
            .OrderByDescending(e => e.ImplantDate)
            .Select(e => new {
                e.EmbryoRecordId, e.Code, e.Donor, e.Sire, e.Grade, e.Status,
                e.ImplantDate, e.RecipientAnimalId,
                RecipientName = e.RecipientAnimal == null ? null :
                    e.RecipientAnimal.BarnName ?? e.RecipientAnimal.RegisteredName
            }).ToListAsync();

        return Ok(new {
            GeneratedAt = DateTime.UtcNow,
            Animals = animals,
            MissingRegistration = animals.Where(a => string.IsNullOrWhiteSpace(a.RegistrationNumber)),
            DueWithinEightMonths = breedings.Where(b =>
                b.PregnancyStatus == PregnancyStatus.Pregnant &&
                b.ExpectedDueDate >= today && b.ExpectedDueDate <= eightMonths),
            LastMonthHeats = heats,
            Breedings = breedings,
            HeiferPregChecks = breedings.Where(b =>
                b.AnimalStage == AnimalStage.Heifer &&
                (b.PregnancyStatus == PregnancyStatus.Unconfirmed || b.PregnancyStatus == PregnancyStatus.Recheck)),
            CowPregChecks = breedings.Where(b =>
                (b.AnimalStage == AnimalStage.Milking || b.AnimalStage == AnimalStage.Dry) &&
                (b.PregnancyStatus == PregnancyStatus.Unconfirmed || b.PregnancyStatus == PregnancyStatus.Recheck)),
            Embryos = embryos,
            AvailableEmbryos = embryos.Where(e =>
                e.Status == EmbryoStatus.InStorage || e.Status == EmbryoStatus.Assigned),
            Implants = embryos.Where(e => e.ImplantDate != null),
            EmbryoStatistics = new {
                Total = embryos.Count,
                InStorage = embryos.Count(e => e.Status == EmbryoStatus.InStorage),
                Implanted = embryos.Count(e => e.Status == EmbryoStatus.Implanted),
                Successful = embryos.Count(e => e.Status == EmbryoStatus.Successful),
                Failed = embryos.Count(e => e.Status == EmbryoStatus.Failed)
            }
        });
    }
}
