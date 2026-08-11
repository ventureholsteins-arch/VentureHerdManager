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
        var todayDateOnly = DateOnly.FromDateTime(today);
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

        var herdData = await context.AnimalDataRecords.AsNoTracking().ToListAsync();
        var latestMilk = herdData
            .Where(r => r.Source == HerdDataSource.Pcdart)
            .GroupBy(r => r.AnimalId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.ReportDate).First());
        var latestGenomics = herdData
            .Where(r => r.Source == HerdDataSource.Zoetis)
            .GroupBy(r => r.AnimalId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.ReportDate).First());
        var milkValues = latestMilk.Values.Where(r => r.Milk.HasValue).Select(r => r.Milk!.Value).OrderBy(v => v).ToList();
        var netMeritValues = latestGenomics.Values.Where(r => r.NetMerit.HasValue).Select(r => r.NetMerit!.Value).OrderBy(v => v).ToList();
        static decimal LowRank<T>(T value, List<T> sorted) where T : IComparable<T>
        {
            if (sorted.Count < 2) return 0m;
            var index = sorted.FindIndex(candidate => candidate.CompareTo(value) >= 0);
            if (index < 0) index = sorted.Count - 1;
            return 1m - (decimal)index / (sorted.Count - 1);
        }
        var suggestedSell = animals
            .Where(a => a.AnimalStage == AnimalStage.Milking)
            .Select(animal =>
            {
                latestMilk.TryGetValue(animal.AnimalId, out var milk);
                latestGenomics.TryGetValue(animal.AnimalId, out var genomic);
                var breeding = currentBreedings.FirstOrDefault(b => b.AnimalId == animal.AnimalId);
                var openCount = breedings.Count(b => b.AnimalId == animal.AnimalId && b.PregnancyStatus == PregnancyStatus.Open);
                var ageYears = animal.BirthDate.HasValue
                    ? (today - animal.BirthDate.Value.ToDateTime(TimeOnly.MinValue)).TotalDays / 365.25
                    : 0;
                var score = 0;
                var concerns = new List<string>();
                var strengths = new List<string>();

                if (milk?.Milk is decimal milkPounds)
                {
                    var points = (int)Math.Round(LowRank(milkPounds, milkValues) * 35m);
                    score += points;
                    if (points >= 22) concerns.Add($"Low herd milk rank ({milkPounds:0.#})");
                    else if (points <= 8) strengths.Add($"Strong herd milk rank ({milkPounds:0.#})");
                }
                else concerns.Add("No current PC-DART milk value");

                if (genomic?.NetMerit is int netMerit)
                {
                    var points = (int)Math.Round(LowRank(netMerit, netMeritValues) * 20m);
                    score += points;
                    if (points >= 13) concerns.Add($"Lower genomic NM$ ({netMerit})");
                    else if (points <= 5) strengths.Add($"Strong genomic NM$ ({netMerit})");
                }
                else concerns.Add("No Zoetis genomic match");

                var pregnant = breeding?.PregnancyStatus == PregnancyStatus.Pregnant;
                if (pregnant) strengths.Add("Currently pregnant");
                else
                {
                    score += 20;
                    concerns.Add(breeding == null ? "No current breeding" : $"Current repro status: {breeding.PregnancyStatus}");
                }
                if (openCount >= 2) { score += Math.Min(15, openCount * 5); concerns.Add($"{openCount} recorded open checks"); }
                if (ageYears >= 6) { score += Math.Min(10, (int)Math.Floor(ageYears - 5) * 2); concerns.Add($"Age {ageYears:0.0} years"); }

                score = Math.Min(100, score);
                return new
                {
                    animal.AnimalId, animal.BarnName, animal.RegisteredName, animal.RegistrationNumber,
                    animal.BirthDate, animal.SireName, animal.DamName,
                    Score = score,
                    ReviewLevel = score >= 55 ? "Review first" : score >= 35 ? "Watch closely" : "Lower concern",
                    Milk = milk?.Milk,
                    DaysInMilk = milk?.DaysInMilk,
                    NetMerit = genomic?.NetMerit,
                    Tpi = genomic?.Tpi,
                    ReproStatus = breeding?.PregnancyStatus.ToString() ?? "Not bred",
                    Concerns = concerns,
                    Strengths = strengths,
                    DataComplete = milk != null && genomic != null
                };
            })
            .OrderByDescending(row => row.Score)
            .ThenBy(row => row.BarnName)
            .ToList();

        var saleAnimals = animals
            .Select(animal =>
            {
                var breeding = currentBreedings.FirstOrDefault(b => b.AnimalId == animal.AnimalId);
                var monthsOld = animal.BirthDate.HasValue
                    ? Math.Max(0,
                        (todayDateOnly.Year - animal.BirthDate.Value.Year) * 12
                        + todayDateOnly.Month - animal.BirthDate.Value.Month
                        - (todayDateOnly.Day < animal.BirthDate.Value.Day ? 1 : 0))
                    : (int?)null;

                return new
                {
                    animal.AnimalId,
                    animal.BarnName,
                    animal.RegisteredName,
                    animal.RegistrationNumber,
                    animal.BirthDate,
                    animal.SireName,
                    animal.DamName,
                    OpenStatus = breeding?.PregnancyStatus.ToString() ?? "Not bred",
                    MonthsOld = monthsOld,
                    TimesBred = breedings.Count(b => b.AnimalId == animal.AnimalId)
                };
            })
            .OrderBy(row => row.BarnName)
            .ThenBy(row => row.RegisteredName)
            .ToList();

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
            SaleAnimals = saleAnimals,
            SuggestedSell = suggestedSell,
            PregnancyChecksDue = pregnancyChecksDue,
            DueWithinEightMonths = currentBreedings.Where(b =>
                b.PregnancyStatus == PregnancyStatus.Pregnant
                && b.ExpectedDueDate >= today
                && b.ExpectedDueDate <= eightMonths)
                .OrderBy(b => b.ExpectedDueDate)
                .ThenBy(b => b.AnimalName),
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
