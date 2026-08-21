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
public sealed class HerdDataController(HerdDataImportService importer, ApplicationDbContext context, HerdDataAdminAccess admin, ILogger<HerdDataController> logger) : ControllerBase
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
        catch (Exception exception)
        {
            logger.LogError(exception, "Confirmed herd import {FileName} could not be saved", request.FileName);
            return Problem(title: "The confirmed import could not be saved.", detail: exception.GetBaseException().Message, statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    [HttpGet("animal/{animalId:int}")]
    public async Task<IActionResult> AnimalHistory(int animalId, CancellationToken ct)
    {
        var denied = Guard(); if (denied != null) return denied;
        return Ok(await context.AnimalDataRecords.AsNoTracking().Where(record => record.AnimalId == animalId).OrderByDescending(record => record.ReportDate).ToListAsync(ct));
    }

    [HttpPost("consolidate-milk-tests")]
    public async Task<IActionResult> ConsolidateMilkTests(ConsolidateMilkTestsRequest request, CancellationToken ct)
    {
        var denied = Guard(); if (denied != null) return denied;
        if (request.SourceDate == request.KeepDate) return BadRequest("Choose two different report dates.");

        var source = await context.AnimalDataRecords
            .Where(record => record.Source == HerdDataSource.Pcdart && record.ReportDate == request.SourceDate)
            .ToListAsync(ct);
        var keep = await context.AnimalDataRecords
            .Where(record => record.Source == HerdDataSource.Pcdart && record.ReportDate == request.KeepDate)
            .ToListAsync(ct);
        if (source.Count == 0 || keep.Count == 0) return NotFound("Both milk-test dates must exist before they can be consolidated.");

        var sourceByAnimal = source.GroupBy(record => record.AnimalId).ToDictionary(group => group.Key, group => group.Single());
        var keepByAnimal = keep.GroupBy(record => record.AnimalId).ToDictionary(group => group.Key, group => group.Single());
        if (!sourceByAnimal.Keys.OrderBy(value => value).SequenceEqual(keepByAnimal.Keys.OrderBy(value => value)))
            return Conflict("The two reports do not contain the same cows, so they were not merged.");

        foreach (var (animalId, oldRecord) in sourceByAnimal)
        {
            var keptRecord = keepByAnimal[animalId];
            if (oldRecord.Milk != keptRecord.Milk)
                return Conflict($"Milk differs for animal #{animalId}, so the reports were not merged.");
            keptRecord.DaysInMilk ??= oldRecord.DaysInMilk;
            keptRecord.FatPercent ??= oldRecord.FatPercent;
            keptRecord.ProteinPercent ??= oldRecord.ProteinPercent;
            keptRecord.LastCalvingDate ??= oldRecord.LastCalvingDate;
            keptRecord.OfficialId ??= oldRecord.OfficialId;
        }

        var affectedImportIds = source.Select(record => record.HerdDataImportId).Distinct().ToList();
        context.AnimalDataRecords.RemoveRange(source);
        await context.SaveChangesAsync(ct);

        var emptyImports = await context.HerdDataImports
            .Where(import => affectedImportIds.Contains(import.HerdDataImportId)
                && !import.Records.Any()
                && !import.LifetimeProductionSnapshots.Any())
            .ToListAsync(ct);
        context.HerdDataImports.RemoveRange(emptyImports);
        await context.SaveChangesAsync(ct);
        return Ok(new { keptDate = request.KeepDate, removedDuplicateDate = request.SourceDate, cowsMerged = source.Count });
    }

    [HttpGet("analytics")]
    public async Task<IActionResult> Analytics(CancellationToken ct)
    {
        var denied = Guard(); if (denied != null) return denied;
        var records = await context.AnimalDataRecords.AsNoTracking().Include(r => r.Animal).ToListAsync(ct);
        static string NormalizeTraitKey(string value) => new(value.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());
        static decimal? RawTrait(AnimalDataRecord record, params string[] aliases)
        {
            try
            {
                var normalizedAliases = aliases.Select(NormalizeTraitKey).ToHashSet(StringComparer.Ordinal);
                using var document = JsonDocument.Parse(record.RawDataJson);
                foreach (var property in document.RootElement.EnumerateObject())
                    if (normalizedAliases.Contains(NormalizeTraitKey(property.Name))
                        && decimal.TryParse(property.Value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var value)) return value;
            }
            catch (JsonException) { }
            return null;
        }
        var milkDate = records.Where(r => r.Source == HerdDataSource.Pcdart).Max(r => (DateOnly?)r.ReportDate);
        var genomicDate = records.Where(r => r.Source == HerdDataSource.Zoetis).Max(r => (DateOnly?)r.ReportDate);
        var latestComponentsByAnimal = records
            .Where(record => record.Source == HerdDataSource.Pcdart && (record.FatPercent.HasValue || record.ProteinPercent.HasValue))
            .GroupBy(record => record.AnimalId)
            .ToDictionary(
                group => group.Key,
                group => group.OrderByDescending(record => record.ReportDate).First());
        var milk = records.Where(r => r.Source == HerdDataSource.Pcdart && r.ReportDate == milkDate).OrderByDescending(r => r.Milk).Select(r =>
        {
            latestComponentsByAnimal.TryGetValue(r.AnimalId, out var component);
            var fat = r.FatPercent ?? component?.FatPercent; var protein = r.ProteinPercent ?? component?.ProteinPercent;
            return new { r.AnimalId, AnimalName = r.Animal.DisplayName, r.Animal.SireName, r.Animal.CurrentLactation, r.ReportDate, ComponentReportDate = component?.ReportDate, r.DaysInMilk, r.Milk, FatPercent = fat, ProteinPercent = protein, FatPounds = r.Milk.HasValue && fat.HasValue ? Math.Round(r.Milk.Value * fat.Value / 100m, 2) : (decimal?)null, ProteinPounds = r.Milk.HasValue && protein.HasValue ? Math.Round(r.Milk.Value * protein.Value / 100m, 2) : (decimal?)null };
        }).ToList();
        var sireMilk = milk.Where(r => !string.IsNullOrWhiteSpace(r.SireName)).GroupBy(r => r.SireName!.Trim(), StringComparer.OrdinalIgnoreCase).Select(group => new { sireName = group.Key, daughters = group.Select(r => r.AnimalId).Distinct().Count(), averageMilk = group.Where(r => r.Milk.HasValue).Select(r => r.Milk).Average(), averageFatPercent = group.Where(r => r.FatPercent.HasValue).Select(r => r.FatPercent).Average(), averageProteinPercent = group.Where(r => r.ProteinPercent.HasValue).Select(r => r.ProteinPercent).Average(), averageFatPounds = group.Where(r => r.FatPounds.HasValue).Select(r => r.FatPounds).Average(), averageProteinPounds = group.Where(r => r.ProteinPounds.HasValue).Select(r => r.ProteinPounds).Average() }).OrderByDescending(r => r.averageMilk).ToList();
        var milkHistory = records.Where(r => r.Source == HerdDataSource.Pcdart).GroupBy(r => r.ReportDate).OrderBy(group => group.Key).Select(group => new { reportDate = group.Key, cows = group.Select(r => r.AnimalId).Distinct().Count(), averageMilk = group.Where(r => r.Milk.HasValue).Select(r => r.Milk).Average(), averageFatPercent = group.Where(r => r.FatPercent.HasValue).Select(r => r.FatPercent).Average(), averageProteinPercent = group.Where(r => r.ProteinPercent.HasValue).Select(r => r.ProteinPercent).Average() }).ToList();
        var genomicAll = records.Where(r => r.Source == HerdDataSource.Zoetis && r.ReportDate == genomicDate).OrderByDescending(r => r.Tpi).Select(r => new
        {
            r.AnimalId, AnimalName = r.Animal.DisplayName, r.Animal.AnimalStage, r.ReportDate,
            r.Tpi, r.NetMerit, r.MilkPta, r.FatPta, r.ProteinPta, r.SomaticCellScore,
            r.DaughterPregnancyRate, r.ProductiveLife, r.TypeScore, r.UdderComposite, r.FeetLegsComposite,
            BodyComposite = RawTrait(r, "BDC"), DairyWellnessProfit = RawTrait(r, "DWP$"), CalfWellness = RawTrait(r, "CA$"),
            FeedEfficiency = RawTrait(r, "FE"), ResidualFeedIntake = RawTrait(r, "RFI"), MilkingSpeed = RawTrait(r, "MSPD"),
            FatPercentPta = RawTrait(r, "FAT %"), ProteinPercentPta = RawTrait(r, "PROT%"),
            HeiferConceptionRate = RawTrait(r, "HCR"), CowConceptionRate = RawTrait(r, "CCR"), FertilityIndex = RawTrait(r, "FI"),
            Livability = RawTrait(r, "LIV"), HealthCostIndex = RawTrait(r, "HCC"),
            SireCalvingEase = RawTrait(r, "SCE"), DaughterCalvingEase = RawTrait(r, "DCE"),
            SireStillbirth = RawTrait(r, "SSB"), DaughterStillbirth = RawTrait(r, "DSB"),
            GestationLength = RawTrait(r, "GL"), EarlyFirstCalving = RawTrait(r, "EFC"),
            Stature = RawTrait(r, "ST"), Strength = RawTrait(r, "SG", "STRENGTH", "BODY STRENGTH"), BodyDepth = RawTrait(r, "BD"), DairyForm = RawTrait(r, "DF"),
            RumpAngle = RawTrait(r, "RA"), RumpWidth = RawTrait(r, "RW"), RearLegsSide = RawTrait(r, "LS"), RearLegsRear = RawTrait(r, "LR"),
            FootAngle = RawTrait(r, "FA"), FeetLegsScore = RawTrait(r, "FLS"), ForeUdderAttachment = RawTrait(r, "FU"),
            RearUdderHeight = RawTrait(r, "UH", "RUH"), RearUdderWidth = RawTrait(r, "UW", "RUW"), UdderCleft = RawTrait(r, "UC"),
            UdderDepth = RawTrait(r, "UD"), FrontTeatPlacement = RawTrait(r, "FT"), RearTeatPlacement = RawTrait(r, "RT"), TeatLength = RawTrait(r, "TL")
        }).ToList();
        var genomic = genomicAll.Where(r => r.AnimalStage != AnimalStage.Bull).ToList();
        var bulls = genomicAll.Where(r => r.AnimalStage == AnimalStage.Bull).ToList();
        var genomicHistory = records.Where(r => r.Source == HerdDataSource.Zoetis && r.Animal.AnimalStage != AnimalStage.Bull)
            .GroupBy(r => r.ReportDate).OrderBy(group => group.Key).Select(group => new { reportDate = group.Key, animals = group.Count(), averageTpi = group.Where(r => r.Tpi.HasValue).Select(r => (double?)r.Tpi).Average(), averageNetMerit = group.Where(r => r.NetMerit.HasValue).Select(r => (double?)r.NetMerit).Average(), averageType = group.Where(r => r.TypeScore.HasValue).Select(r => (double?)r.TypeScore).Average(), averageUdder = group.Where(r => r.UdderComposite.HasValue).Select(r => (double?)r.UdderComposite).Average(), averageFeetLegs = group.Where(r => r.FeetLegsComposite.HasValue).Select(r => (double?)r.FeetLegsComposite).Average(), averageFertility = group.Where(r => r.DaughterPregnancyRate.HasValue).Select(r => (double?)r.DaughterPregnancyRate).Average() }).ToList();
        var lifetimeSnapshots = new List<LifetimeProductionSnapshot>();
        try { lifetimeSnapshots = await context.Set<LifetimeProductionSnapshot>().AsNoTracking().Include(snapshot => snapshot.Animal).OrderByDescending(snapshot => snapshot.ReportDate).ToListAsync(ct); }
        catch (Exception) { }
        var lifetimeProduction = lifetimeSnapshots.Select(snapshot => new { snapshot.AnimalId, AnimalName = snapshot.Animal.DisplayName, snapshot.Animal.SireName, snapshot.ReportDate, snapshot.LifetimeMilk, snapshot.LifetimeFat, snapshot.LifetimeProtein, snapshot.Lactations }).ToList();
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
        var latestMilkByAnimal = milk.ToDictionary(row => row.AnimalId);
        var milkDropByAnimal = droppingMilk.ToDictionary(row => row.AnimalId);
        var dryOffWatch = activeAnimals.Where(animal =>
                animal.AnimalStage != AnimalStage.Dry
                && latestBreeding.TryGetValue(animal.AnimalId, out var breeding)
                && breeding.PregnancyStatus == PregnancyStatus.Pregnant
                && breeding.RecommendedDryOffDate.HasValue
                && breeding.RecommendedDryOffDate.Value.Date <= today.AddDays(60)
                && (!latestMilkByAnimal.TryGetValue(animal.AnimalId, out var milkRow)
                    || !milkRow.DaysInMilk.HasValue
                    || milkRow.DaysInMilk.Value >= 200))
            .Select(animal =>
            {
                var breeding = latestBreeding[animal.AnimalId];
                latestMilkByAnimal.TryGetValue(animal.AnimalId, out var milkRow);
                milkDropByAnimal.TryGetValue(animal.AnimalId, out var dropRow);
                return new
                {
                    animal.AnimalId,
                    AnimalName = animal.DisplayName,
                    breeding.RecommendedDryOffDate,
                    breeding.ExpectedDueDate,
                    PregnancyStatus = breeding.PregnancyStatus.ToString(),
                    DaysInMilk = milkRow?.DaysInMilk,
                    Milk = milkRow?.Milk,
                    PreviousMilk = dropRow?.PreviousMilk,
                    MilkDropPercent = dropRow?.DropPercent,
                    DaysUntilDry = (breeding.RecommendedDryOffDate!.Value.Date - today).Days
                };
            })
            .OrderBy(row => row.RecommendedDryOffDate).ToList();
        var classificationRecords = await context.ClassificationRecords.AsNoTracking().Include(record => record.Animal).Where(record => record.Animal != null).ToListAsync(ct);
        var latestClassifications = classificationRecords.GroupBy(record => record.AnimalId).Select(group => group.OrderByDescending(record => record.ClassificationDate ?? record.CreatedAt).First()).Select(record => new { record.AnimalId, AnimalName = record.Animal!.DisplayName, record.Animal.SireName, record.Animal.CurrentLactation, record.Score, record.Baa, record.AgeInMonthsAtScoring, record.ClassificationLabel, Date = record.ClassificationDate ?? record.CreatedAt }).OrderByDescending(record => record.Score).ToList();
        var classificationByYear = classificationRecords.GroupBy(record => (record.ClassificationDate ?? record.CreatedAt).Year).OrderBy(group => group.Key).Select(group => new { year = group.Key, records = group.Count(), animals = group.Select(record => record.AnimalId).Distinct().Count(), averageScore = group.Average(record => record.Score), averageBaa = group.Where(record => record.Baa.HasValue).Select(record => record.Baa).Average() }).ToList();
        var classificationByAge = latestClassifications.Where(record => record.AgeInMonthsAtScoring.HasValue).GroupBy(record => record.AgeInMonthsAtScoring < 36 ? "Under 3 years" : record.AgeInMonthsAtScoring < 48 ? "3-year-olds" : record.AgeInMonthsAtScoring < 60 ? "4-year-olds" : "5 years & over").Select(group => new { ageGroup = group.Key, animals = group.Count(), averageScore = group.Average(record => record.Score), averageBaa = group.Where(record => record.Baa.HasValue).Select(record => record.Baa).Average() }).OrderBy(group => group.ageGroup).ToList();
        var classificationBySire = latestClassifications.Where(record => !string.IsNullOrWhiteSpace(record.SireName)).GroupBy(record => record.SireName!.Trim(), StringComparer.OrdinalIgnoreCase).Select(group => new { sireName = group.Key, daughters = group.Count(), averageScore = group.Average(record => record.Score), averageBaa = group.Where(record => record.Baa.HasValue).Select(record => record.Baa).Average(), highScore = group.Max(record => record.Score) }).OrderByDescending(group => group.averageScore).ToList();
        var secondLactationPlus = latestClassifications.Where(record => (record.CurrentLactation ?? 0) >= 2).OrderByDescending(record => record.Score).ToList();
        var classificationDistribution = latestClassifications.GroupBy(record => record.Score >= 90 ? "Excellent" : record.Score >= 85 ? "Very Good" : record.Score >= 80 ? "Good Plus" : "Below 80").Select(group => new { label = group.Key, animals = group.Count(), averageScore = group.Average(record => record.Score) }).ToList();
        var classification = new { latest = latestClassifications, byYear = classificationByYear, byAge = classificationByAge, bySire = classificationBySire, secondLactationPlus, distribution = classificationDistribution };
        return Ok(new { latestMilkDate = milkDate, latestGenomicDate = genomicDate, milk, sireMilk, milkHistory, lifetimeProduction, genomic, bulls, genomicHistory, combined, classification, attention = new { highDimOpen, longOpenHeifers, droppingMilk, dryOffWatch } });
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

public sealed class ConsolidateMilkTestsRequest
{
    public DateOnly SourceDate { get; set; }
    public DateOnly KeepDate { get; set; }
}
