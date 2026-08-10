using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.Json;
using VentureHerdManager.Api.Data;
using VentureHerdManager.Api.DTOs;
using VentureHerdManager.Api.Models;
using VentureHerdManager.Api.Services;

namespace VentureHerdManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class HerdDataController(HerdDataImportService importer, ApplicationDbContext context, HerdDataAdminAccess admin) : ControllerBase
{
    private IActionResult? Guard() => admin.IsAuthorized(Request) ? null : Unauthorized("Admin access is required for herd production and genomic data.");

    [HttpPost("unlock")]
    public IActionResult Unlock() => Guard() ?? Ok(new { unlocked = true });

    [HttpPost("preview")]
    public async Task<IActionResult> Preview(HerdDataImportRequest request, CancellationToken ct) => Guard() ?? Ok(await importer.PreviewAsync(request, ct));

    [HttpPost("apply")]
    public async Task<IActionResult> Apply(HerdDataImportRequest request, CancellationToken ct)
    {
        var denied = Guard(); if (denied != null) return denied;
        try
        {
            var imported = await importer.ApplyAsync(request, ct);
            return Ok(new { imported.HerdDataImportId, imported.Source, imported.FileName, imported.ReportDate, imported.RowsImported, imported.ImportedAt });
        }
        catch (InvalidOperationException exception) { return BadRequest(exception.Message); }
    }

    [HttpGet("animal/{animalId:int}")]
    public async Task<IActionResult> AnimalHistory(int animalId, CancellationToken ct)
    {
        var denied = Guard(); if (denied != null) return denied;
        return Ok(await context.AnimalDataRecords.AsNoTracking().Where(record => record.AnimalId == animalId).OrderByDescending(record => record.ReportDate).ToListAsync(ct));
    }

    [HttpGet("analytics")]
    public async Task<IActionResult> Analytics(CancellationToken ct)
    {
        var denied = Guard(); if (denied != null) return denied;
        var records = await context.AnimalDataRecords.AsNoTracking().Include(r => r.Animal).ToListAsync(ct);
        static decimal? RawTrait(AnimalDataRecord record, params string[] aliases)
        {
            try
            {
                using var document = JsonDocument.Parse(record.RawDataJson);
                foreach (var property in document.RootElement.EnumerateObject())
                    if (aliases.Any(alias => string.Equals(property.Name.Trim(), alias, StringComparison.OrdinalIgnoreCase))
                        && decimal.TryParse(property.Value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var value)) return value;
            }
            catch (JsonException) { }
            return null;
        }
        var milkDate = records.Where(r => r.Source == HerdDataSource.Pcdart).Max(r => (DateOnly?)r.ReportDate);
        var genomicDate = records.Where(r => r.Source == HerdDataSource.Zoetis).Max(r => (DateOnly?)r.ReportDate);
        var milk = records.Where(r => r.Source == HerdDataSource.Pcdart && r.ReportDate == milkDate).OrderByDescending(r => r.Milk).Select(r => new { r.AnimalId, AnimalName = r.Animal.DisplayName, r.ReportDate, r.DaysInMilk, r.Milk, r.FatPercent, r.ProteinPercent }).ToList();
        var genomicAll = records.Where(r => r.Source == HerdDataSource.Zoetis && r.ReportDate == genomicDate).OrderByDescending(r => r.Tpi).Select(r => new { r.AnimalId, AnimalName = r.Animal.DisplayName, r.Animal.AnimalStage, r.ReportDate, r.Tpi, r.NetMerit, r.MilkPta, r.DaughterPregnancyRate, r.ProductiveLife, r.TypeScore, r.UdderComposite, r.FeetLegsComposite, RearUdderHeight = RawTrait(r, "RUH", "REAR UDDER HEIGHT", "REAR UDDER HT"), RearUdderWidth = RawTrait(r, "RUW", "REAR UDDER WIDTH"), Strength = RawTrait(r, "STR", "STRENGTH") }).ToList();
        var genomic = genomicAll.Where(r => r.AnimalStage != AnimalStage.Bull).ToList();
        var bulls = genomicAll.Where(r => r.AnimalStage == AnimalStage.Bull).ToList();
        var genomicHistory = records.Where(r => r.Source == HerdDataSource.Zoetis && r.Animal.AnimalStage != AnimalStage.Bull)
            .GroupBy(r => r.ReportDate).OrderBy(group => group.Key).Select(group => new { reportDate = group.Key, animals = group.Count(), averageTpi = group.Where(r => r.Tpi.HasValue).Select(r => (double?)r.Tpi).Average(), averageNetMerit = group.Where(r => r.NetMerit.HasValue).Select(r => (double?)r.NetMerit).Average(), averageType = group.Where(r => r.TypeScore.HasValue).Select(r => (double?)r.TypeScore).Average(), averageUdder = group.Where(r => r.UdderComposite.HasValue).Select(r => (double?)r.UdderComposite).Average(), averageFeetLegs = group.Where(r => r.FeetLegsComposite.HasValue).Select(r => (double?)r.FeetLegsComposite).Average(), averageFertility = group.Where(r => r.DaughterPregnancyRate.HasValue).Select(r => (double?)r.DaughterPregnancyRate).Average() }).ToList();
        var combined = milk.Join(genomic, m => m.AnimalId, g => g.AnimalId, (m, g) => new { m.AnimalId, m.AnimalName, m.Milk, m.DaysInMilk, g.Tpi, g.NetMerit, g.MilkPta, g.DaughterPregnancyRate, g.ProductiveLife, g.TypeScore, g.UdderComposite, g.FeetLegsComposite }).OrderBy(x => x.Milk).ToList();
        var activeAnimals = await context.Animals.AsNoTracking().Where(a => a.AnimalStatus == AnimalStatus.Active).ToListAsync(ct);
        var breedingEvents = await context.BreedingEvents.AsNoTracking().OrderByDescending(b => b.BreedingDate).ToListAsync(ct);
        var latestBreeding = breedingEvents.GroupBy(b => b.AnimalId).ToDictionary(group => group.Key, group => group.First());
        var milkByAnimal = records.Where(r => r.Source == HerdDataSource.Pcdart && r.Milk.HasValue)
            .GroupBy(r => r.AnimalId).ToDictionary(group => group.Key, group => group.OrderByDescending(r => r.ReportDate).ToList());
        var today = DateTime.UtcNow.Date;
        var highDimOpen = milk.Where(row => (row.DaysInMilk ?? 0) >= 200)
            .Where(row => !latestBreeding.TryGetValue(row.AnimalId, out var breeding) || breeding.PregnancyStatus != PregnancyStatus.Pregnant)
            .OrderByDescending(row => row.DaysInMilk).ToList();
        var longOpenHeifers = activeAnimals.Where(animal => animal.AnimalStage == AnimalStage.Heifer && animal.BirthDate.HasValue)
            .Where(animal => animal.BirthDate!.Value.ToDateTime(TimeOnly.MinValue) <= today.AddMonths(-15))
            .Where(animal => !latestBreeding.TryGetValue(animal.AnimalId, out var breeding) || breeding.PregnancyStatus != PregnancyStatus.Pregnant)
            .Select(animal => new { animal.AnimalId, AnimalName = animal.DisplayName, animal.BirthDate, AgeMonths = (int)((today - animal.BirthDate!.Value.ToDateTime(TimeOnly.MinValue)).TotalDays / 30.44), LastBred = latestBreeding.TryGetValue(animal.AnimalId, out var breeding) ? breeding.BreedingDate : (DateTime?)null })
            .OrderByDescending(row => row.AgeMonths).ToList();
        var droppingMilk = milkByAnimal.Where(pair => pair.Value.Count >= 2)
            .Select(pair => new { AnimalId = pair.Key, AnimalName = pair.Value[0].Animal.DisplayName, CurrentMilk = pair.Value[0].Milk, PreviousMilk = pair.Value[1].Milk, pair.Value[0].ReportDate, DropPercent = pair.Value[1].Milk > 0 ? Math.Round(((pair.Value[1].Milk!.Value - pair.Value[0].Milk!.Value) / pair.Value[1].Milk.Value) * 100m, 1) : 0m })
            .Where(row => row.DropPercent >= 10m).OrderByDescending(row => row.DropPercent).ToList();
        var dryOffWatch = activeAnimals.Where(animal => animal.AnimalStage != AnimalStage.Dry && latestBreeding.TryGetValue(animal.AnimalId, out var breeding) && breeding.PregnancyStatus == PregnancyStatus.Pregnant && breeding.RecommendedDryOffDate.HasValue && breeding.RecommendedDryOffDate.Value.Date <= today.AddDays(60))
            .Select(animal => { var breeding = latestBreeding[animal.AnimalId]; return new { animal.AnimalId, AnimalName = animal.DisplayName, breeding.RecommendedDryOffDate, breeding.ExpectedDueDate, DaysUntilDry = (breeding.RecommendedDryOffDate!.Value.Date - today).Days }; })
            .OrderBy(row => row.RecommendedDryOffDate).ToList();
        return Ok(new { latestMilkDate = milkDate, latestGenomicDate = genomicDate, milk, genomic, bulls, genomicHistory, combined, attention = new { highDimOpen, longOpenHeifers, droppingMilk, dryOffWatch } });
    }

    [HttpGet("mating/{animalId:int}")]
    public async Task<IActionResult> Mating(int animalId, CancellationToken ct)
    {
        var denied = Guard(); if (denied != null) return denied;
        var cow = await context.AnimalDataRecords.AsNoTracking().Where(r => r.AnimalId == animalId && r.Source == HerdDataSource.Zoetis).OrderByDescending(r => r.ReportDate).FirstOrDefaultAsync(ct);
        if (cow == null) return NotFound("No genomic evaluation is stored for this animal.");
        static decimal Need(decimal? value) => Math.Max(0m, 1m - (value ?? 0m));
        var sires = await context.SireReferences.AsNoTracking().ToListAsync(ct);
        var rankedSires = sires.Select(sire => new
        {
            sire.SireReferenceId, sire.Name, sire.NaabCode, sire.NetMerit, sire.PtaMilk, sire.DaughterPregnancyRate, sire.ProductiveLife, sire.PtaType, sire.UdderComposite, sire.FeetLegsComposite,
            Score = Need(cow.UdderComposite) * (sire.UdderComposite ?? 0m) + Need(cow.FeetLegsComposite) * (sire.FeetLegsComposite ?? 0m) + Need(cow.TypeScore) * (sire.PtaType ?? 0m) + Need(cow.DaughterPregnancyRate) * (sire.DaughterPregnancyRate ?? 0m) + Need(cow.ProductiveLife) * (sire.ProductiveLife ?? 0m),
            Reasons = new[] {
                (cow.UdderComposite ?? 0) < 1 && (sire.UdderComposite ?? 0) > 0 ? "Udder composite improvement" : null,
                (cow.FeetLegsComposite ?? 0) < 1 && (sire.FeetLegsComposite ?? 0) > 0 ? "Feet & legs improvement" : null,
                (cow.DaughterPregnancyRate ?? 0) < 0 && (sire.DaughterPregnancyRate ?? 0) > 0 ? "Fertility improvement" : null,
                (cow.ProductiveLife ?? 0) < 0 && (sire.ProductiveLife ?? 0) > 0 ? "Productive-life improvement" : null
            }.Where(reason => reason != null),
            Concerns = new[] {
                (cow.UdderComposite ?? 0) < 1 && (sire.UdderComposite ?? 0) <= 0 ? "Does not improve this cow's weak udder composite" : null,
                (cow.FeetLegsComposite ?? 0) < 1 && (sire.FeetLegsComposite ?? 0) <= 0 ? "Does not improve this cow's weak feet & legs" : null,
                (cow.DaughterPregnancyRate ?? 0) < 0 && (sire.DaughterPregnancyRate ?? 0) <= 0 ? "Could compound a fertility weakness" : null,
                (cow.ProductiveLife ?? 0) < 0 && (sire.ProductiveLife ?? 0) <= 0 ? "Could compound productive-life weakness" : null
            }.Where(reason => reason != null)
        }).ToList();
        var suggestions = rankedSires.OrderByDescending(item => item.Score).ThenByDescending(item => item.NetMerit).Take(20).ToList();
        var avoid = rankedSires.Where(item => item.Concerns.Any()).OrderBy(item => item.Score).ThenBy(item => item.NetMerit).Take(10).ToList();
        return Ok(new { cow = new { cow.AnimalId, cow.ReportDate, cow.Tpi, cow.NetMerit, cow.MilkPta, cow.DaughterPregnancyRate, cow.ProductiveLife, cow.TypeScore, cow.UdderComposite, cow.FeetLegsComposite }, suggestions, avoid });
    }
}
