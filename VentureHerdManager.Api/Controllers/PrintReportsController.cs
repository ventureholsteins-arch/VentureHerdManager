using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VentureHerdManager.Api.Data;
using VentureHerdManager.Api.Models;
using VentureHerdManager.Api.Services;

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
        var sevenMonthsAgo = DateOnly.FromDateTime(today.AddMonths(-7));

        var animals = await context.Animals.AsNoTracking()
            .Where(a => a.AnimalStatus == AnimalStatus.Active)
            .OrderBy(a => a.AnimalStage).ThenBy(a => a.BarnName)
            .Select(a => new
            {
                a.AnimalId,
                a.BarnName,
                a.RegisteredName,
                a.RegistrationNumber,
                a.BirthDate,
                a.AnimalStage,
                a.Breed,
                a.SireName,
                a.DamName
            })
            .ToListAsync();

        var heats = await context.HeatEvents.AsNoTracking()
            .Where(h => h.HeatDateTime >= monthAgo)
            .OrderByDescending(h => h.HeatDateTime)
            .Select(h => new
            {
                h.HeatEventId,
                h.AnimalId,
                AnimalName =
                    h.Animal!.BarnName
                    ?? h.Animal.RegisteredName
                    ?? $"Animal #{h.AnimalId}",
                h.HeatDateTime,
                h.Notes
            })
            .ToListAsync();

        var breedings = await context.BreedingEvents.AsNoTracking()
            .OrderByDescending(b => b.BreedingDate)
            .Select(b => new
            {
                b.BreedingEventId,
                b.AnimalId,
                AnimalName =
                    b.Animal!.BarnName
                    ?? b.Animal.RegisteredName
                    ?? $"Animal #{b.AnimalId}",
                AnimalStage = b.Animal.AnimalStage,
                b.BreedingDate,
                b.SireUsed,
                b.BreedingType,
                b.PregnancyStatus,
                b.ExpectedDueDate,
                b.PregnancyCheckDueDate
            })
            .ToListAsync();

        var currentBreedingIds = await context.BreedingEvents
            .AsNoTracking()
            .CurrentReproductiveEvents(context)
            .Select(b => b.BreedingEventId)
            .ToHashSetAsync();
        var currentBreedings = breedings
            .Where(b => currentBreedingIds.Contains(b.BreedingEventId))
            .ToList();

        var embryos = await context.EmbryoRecords.AsNoTracking()
            .OrderByDescending(e => e.ImplantDate)
            .Select(e => new
            {
                e.EmbryoRecordId,
                e.Code,
                e.Donor,
                e.Sire,
                e.Grade,
                e.Status,
                e.ImplantDate,
                e.RecipientAnimalId,
                RecipientName = e.RecipientAnimal == null
                    ? null
                    : e.RecipientAnimal.BarnName
                      ?? e.RecipientAnimal.RegisteredName
            })
            .ToListAsync();

        var currentlyServicedAnimalIds = currentBreedings
            .Where(b => b.PregnancyStatus is not (
                PregnancyStatus.Open or PregnancyStatus.Aborted))
            .Select(b => b.AnimalId)
            .ToHashSet();
        var missingAnimalIdentification = animals.Where(a =>
            string.IsNullOrWhiteSpace(a.BarnName)
            || string.IsNullOrWhiteSpace(a.RegistrationNumber));
        var oldEnoughNotBred = animals.Where(a =>
            a.BirthDate <= sevenMonthsAgo
            && !currentlyServicedAnimalIds.Contains(a.AnimalId));
        var milkingNotBred = animals.Where(a =>
            a.AnimalStage == AnimalStage.Milking
            && !currentlyServicedAnimalIds.Contains(a.AnimalId));
        var pregnancyChecksDue = currentBreedings.Where(b =>
            b.PregnancyCheckDueDate <= today
            && (b.PregnancyStatus == PregnancyStatus.Unconfirmed
                || b.PregnancyStatus == PregnancyStatus.Recheck));
        var siresUsed = breedings
            .Select(breeding => new
            {
                Breeding = breeding,
                Sire = ExtractSireName(breeding.SireUsed)
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.Sire))
            .GroupBy(
                item => NormalizeSire(item.Sire),
                StringComparer.Ordinal)
            .Select(group => new
            {
                Sire = group.First().Sire,
                Breedings = group.Count(),
                Animals = group
                    .Select(item => item.Breeding.AnimalId)
                    .Distinct()
                    .Count(),
                Pregnant = group.Count(item =>
                    item.Breeding.PregnancyStatus
                    == PregnancyStatus.Pregnant),
                Open = group.Count(item =>
                    item.Breeding.PregnancyStatus
                    == PregnancyStatus.Open),
                ToCheck = group.Count(item =>
                    item.Breeding.PregnancyStatus
                        is PregnancyStatus.Unconfirmed
                        or PregnancyStatus.Recheck),
                FirstUsed = group.Min(item =>
                    item.Breeding.BreedingDate),
                LastUsed = group.Max(item =>
                    item.Breeding.BreedingDate)
            })
            .OrderByDescending(item => item.Breedings)
            .ThenBy(item => item.Sire)
            .ToList();

        return Ok(new
        {
            GeneratedAt = DateTime.UtcNow,
            Animals = animals,
            MissingRegistration = animals.Where(a =>
                string.IsNullOrWhiteSpace(a.RegistrationNumber)),
            MissingAnimalIdentification = missingAnimalIdentification,
            OldEnoughNotBred = oldEnoughNotBred,
            MilkingNotBred = milkingNotBred,
            PregnancyChecksDue = pregnancyChecksDue,
            DueWithinEightMonths = currentBreedings.Where(b =>
                b.PregnancyStatus == PregnancyStatus.Pregnant
                && b.ExpectedDueDate >= today
                && b.ExpectedDueDate <= eightMonths),
            LastMonthHeats = heats,
            Breedings = breedings,
            SiresUsed = siresUsed,
            HeiferPregChecks = currentBreedings.Where(b =>
                b.AnimalStage == AnimalStage.Heifer
                && (b.PregnancyStatus == PregnancyStatus.Unconfirmed
                    || b.PregnancyStatus == PregnancyStatus.Recheck)),
            CowPregChecks = currentBreedings.Where(b =>
                (b.AnimalStage == AnimalStage.Milking
                 || b.AnimalStage == AnimalStage.Dry)
                && (b.PregnancyStatus == PregnancyStatus.Unconfirmed
                    || b.PregnancyStatus == PregnancyStatus.Recheck)),
            Embryos = embryos,
            AvailableEmbryos = embryos.Where(e =>
                e.Status == EmbryoStatus.InStorage
                || e.Status == EmbryoStatus.Assigned),
            Implants = embryos.Where(e => e.ImplantDate != null),
            EmbryoStatistics = new
            {
                Total = embryos.Count,
                InStorage = embryos.Count(e =>
                    e.Status == EmbryoStatus.InStorage),
                Implanted = embryos.Count(e =>
                    e.Status == EmbryoStatus.Implanted),
                Successful = embryos.Count(e =>
                    e.Status == EmbryoStatus.Successful),
                Failed = embryos.Count(e =>
                    e.Status == EmbryoStatus.Failed)
            }
        });
    }

    private static string ExtractSireName(string sireUsed)
    {
        var cleaned = sireUsed.Trim();
        foreach (var separator in new[] { " x ", " × " })
        {
            var index = cleaned.LastIndexOf(
                separator,
                StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                return cleaned[(index + separator.Length)..].Trim();
            }
        }

        return cleaned;
    }

    private static string NormalizeSire(string value) =>
        new(
            value
                .Where(char.IsLetterOrDigit)
                .Select(char.ToUpperInvariant)
                .ToArray());
}
