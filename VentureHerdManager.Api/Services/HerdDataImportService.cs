using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VentureHerdManager.Api.Data;
using VentureHerdManager.Api.DTOs;
using VentureHerdManager.Api.Models;

namespace VentureHerdManager.Api.Services;

public sealed class HerdDataImportService(ApplicationDbContext context)
{
    public async Task<HerdDataPreview> PreviewAsync(HerdDataImportRequest request, CancellationToken ct = default)
    {
        var parsed = Parse(request);
        var hash = Hash(request.CsvText);
        var animals = await context.Animals.AsNoTracking().ToListAsync(ct);
        var dryOffs = await context.DryOffEvents.AsNoTracking().ToListAsync(ct);
        var saved = await context.AnimalIdentityMappings.AsNoTracking().Where(m => m.Source == request.Source).ToDictionaryAsync(m => m.SourceKey, ct);
        var sameDateImports = await context.HerdDataImports.AsNoTracking().Where(i => i.Source == request.Source && i.ReportDate == request.ReportDate).ToListAsync(ct);
        var existingImport = sameDateImports.FirstOrDefault(i => i.FileHash == hash || ImportBucket(i.FileName) == ImportBucket(request.FileName));
        var preview = new HerdDataPreview
        {
            Source = request.Source,
            RowsRead = parsed.Count,
            DuplicateImport = existingImport != null,
            ExactDuplicateFile = existingImport?.FileHash == hash,
            ExistingFileName = existingImport?.FileName,
            ExistingRows = existingImport?.RowsImported,
            ExistingImportedAt = existingImport?.ImportedAt
        };
        foreach (var row in parsed)
        {
            var candidates = FindCandidates(row, animals);
            var mappedId = request.AnimalMappings.GetValueOrDefault(row.SourceKey);
            if (mappedId == 0 && saved.TryGetValue(row.SourceKey, out var prior)) mappedId = prior.AnimalId;
            if (mappedId == 0 && candidates.Count == 1) mappedId = candidates[0].AnimalId;
            var animal = animals.FirstOrDefault(a => a.AnimalId == mappedId);
            var previewRow = new HerdDataPreviewRow
            {
                SourceKey = row.SourceKey, SourceName = row.SourceName, OfficialId = row.OfficialId,
                BirthDate = row.BirthDate, Breed = row.Breed, ImportedSex = row.ImportedSex,
                ImportedSire = row.ImportedSire, ImportedDam = row.ImportedDam,
                AnimalId = animal?.AnimalId, AnimalName = animal?.DisplayName,
                NeedsConfirmation = animal == null,
                Candidates = candidates.Take(12).Select(a => new HerdDataCandidate { AnimalId = a.AnimalId, AnimalName = a.DisplayName, RegistrationNumber = a.RegistrationNumber }).ToList()
            };
            AddDryCowAudit(previewRow, row, animal, dryOffs, request.ReportDate);
            AddPedigreeAudit(previewRow, row, animal, request.Source);
            preview.Rows.Add(previewRow);
        }
        return preview;
    }

    public async Task<HerdDataImport> ApplyAsync(HerdDataImportRequest request, CancellationToken ct = default)
    {
        var hash = Hash(request.CsvText);
        var existing = await context.HerdDataImports.Include(i => i.Records).SingleOrDefaultAsync(i => i.FileHash == hash, ct);
        if (existing != null) return existing;
        var sameDateImports = await context.HerdDataImports.Include(i => i.Records)
            .Where(i => i.Source == request.Source && i.ReportDate == request.ReportDate).ToListAsync(ct);
        var sameDateImport = sameDateImports.SingleOrDefault(i => ImportBucket(i.FileName) == ImportBucket(request.FileName));
        if (sameDateImport != null && !request.ConfirmDuplicateReplace)
            throw new InvalidOperationException($"A {request.Source} report for {request.ReportDate:yyyy-MM-dd} is already stored. Review the duplicate warning and explicitly accept replacement or decline it.");
        var priorRecords = sameDateImport?.Records.ToDictionary(record => record.AnimalId) ?? [];
        if (sameDateImport != null) context.HerdDataImports.Remove(sameDateImport);
        var parsed = Parse(request);
        var preview = await PreviewAsync(request, ct);
        if (preview.Rows.Any(r => r.NeedsConfirmation)) throw new InvalidOperationException("Every source row must be matched to a herd animal before import.");
        var batch = new HerdDataImport { Source = request.Source, FileName = request.FileName, FileHash = hash, ReportDate = request.ReportDate };
        context.HerdDataImports.Add(batch);
        for (var index = 0; index < parsed.Count; index++)
        {
            var row = parsed[index];
            var match = preview.Rows[index];
            var record = row.ToRecord(match.AnimalId!.Value, request.ReportDate, request.Source);
            if (priorRecords.TryGetValue(match.AnimalId.Value, out var priorRecord)) MergePriorValues(record, priorRecord);
            batch.Records.Add(record);
            var lifetimeMilk = Dec(row.Values.GetValueOrDefault("Lifetime Milk"));
            var lifetimeFat = Dec(row.Values.GetValueOrDefault("Lifetime Fat"));
            var lifetimeProtein = Dec(row.Values.GetValueOrDefault("Lifetime Protein"));
            if (lifetimeMilk.HasValue || lifetimeFat.HasValue || lifetimeProtein.HasValue)
                batch.LifetimeProductionSnapshots.Add(new LifetimeProductionSnapshot
                {
                    AnimalId = match.AnimalId.Value, ReportDate = request.ReportDate,
                    LifetimeMilk = lifetimeMilk, LifetimeFat = lifetimeFat, LifetimeProtein = lifetimeProtein,
                    Lactations = Int(row.Values.GetValueOrDefault("Lifetime Lactations")), SourceFileName = request.FileName
                });
            var animal = await context.Animals.FindAsync([match.AnimalId.Value], ct);
            if (animal != null)
            {
                await ApplyDryCowDataAsync(animal, row, request.ReportDate, ct);
                await EnrichConfirmedAnimalAsync(animal, row, request.Source, ct);
            }
            var mapping = await context.AnimalIdentityMappings.SingleOrDefaultAsync(m => m.Source == request.Source && m.SourceKey == row.SourceKey, ct);
            if (mapping == null) context.AnimalIdentityMappings.Add(new AnimalIdentityMapping { Source = request.Source, SourceKey = row.SourceKey, SourceLabel = row.SourceName, AnimalId = match.AnimalId.Value });
            else { mapping.AnimalId = match.AnimalId.Value; mapping.SourceLabel = row.SourceName; mapping.ConfirmedAt = DateTime.UtcNow; }
        }
        await ReconcileCurrentPcdartStagesAsync(
            request,
            preview.Rows.Select(row => row.AnimalId!.Value).ToHashSet(),
            ct);
        batch.RowsImported = batch.Records.Count;
        try
        {
            await context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException exception)
        {
            throw new InvalidOperationException($"The confirmed import could not be stored: {exception.GetBaseException().Message}", exception);
        }
        return batch;
    }

    private async Task ReconcileCurrentPcdartStagesAsync(
        HerdDataImportRequest request,
        HashSet<int> currentImportAnimalIds,
        CancellationToken ct)
    {
        if (request.Source != HerdDataSource.Pcdart) return;

        var bucket = ImportBucket(request.FileName);
        if (bucket is not ("CURRENT-MILKING" or "DRY-COWS")) return;

        var currentMilkingIds = bucket == "CURRENT-MILKING"
            ? currentImportAnimalIds
            : await LatestImportAnimalIdsAsync(
                request.ReportDate,
                "CURRENT-MILKING",
                ct);
        var dryCowIds = bucket == "DRY-COWS"
            ? currentImportAnimalIds
            : await LatestImportAnimalIdsAsync(
                request.ReportDate,
                "DRY-COWS",
                ct);

        foreach (var animal in await context.Animals
                     .Where(animal => currentMilkingIds.Contains(animal.AnimalId))
                     .ToListAsync(ct))
        {
            if (animal.AnimalStatus != AnimalStatus.Active
                || animal.AnimalStage == AnimalStage.Milking)
                continue;

            animal.AnimalStage = AnimalStage.Milking;
            animal.UpdatedAt = DateTime.UtcNow;
            animal.UpdatedBy = "PC-DART 005 source-of-truth reconciliation";
        }

        // Do not remove stale stages until both halves of the current cow list
        // have been imported for the same report date. This prevents a 005
        // import from temporarily erasing every dry cow (and vice versa).
        if (currentMilkingIds.Count == 0 || dryCowIds.Count == 0) return;

        var currentCowIds = currentMilkingIds.Concat(dryCowIds).ToHashSet();
        var stale = await context.Animals
            .Where(animal => animal.AnimalStatus == AnimalStatus.Active
                && (animal.AnimalStage == AnimalStage.Milking
                    || animal.AnimalStage == AnimalStage.Dry)
                && !currentCowIds.Contains(animal.AnimalId))
            .ToListAsync(ct);

        foreach (var animal in stale)
        {
            var priorStage = animal.AnimalStage;
            animal.AnimalStage = AnimalStage.Unknown;
            animal.UpdatedAt = DateTime.UtcNow;
            animal.UpdatedBy = "PC-DART current-list reconciliation";
            context.AnimalNotes.Add(new AnimalNote
            {
                AnimalId = animal.AnimalId,
                NoteDate = DateTime.UtcNow,
                NoteType = NoteType.Other,
                NoteText = $"[PC-DART AUDIT] Removed stale {priorStage} stage because this active animal was absent from both the 005 Milking and 024 Dry reports dated {request.ReportDate:MM/dd/yyyy}. Review and set the correct stage if she belongs outside PC-DART.",
                CreatedBy = "PC-DART current-list reconciliation"
            });
        }
    }

    private async Task<HashSet<int>> LatestImportAnimalIdsAsync(
        DateOnly reportDate,
        string bucket,
        CancellationToken ct)
    {
        var import = await context.HerdDataImports
            .AsNoTracking()
            .Include(item => item.Records)
            .Where(item => item.Source == HerdDataSource.Pcdart
                && item.ReportDate == reportDate)
            .OrderByDescending(item => item.ImportedAt)
            .ToListAsync(ct);

        return import
            .FirstOrDefault(item => ImportBucket(item.FileName) == bucket)
            ?.Records.Select(record => record.AnimalId).ToHashSet()
            ?? [];
    }

    private static void MergePriorValues(AnimalDataRecord current, AnimalDataRecord prior)
    {
        current.SourceAnimalId = string.IsNullOrWhiteSpace(current.SourceAnimalId) ? prior.SourceAnimalId : current.SourceAnimalId;
        current.SourceAnimalName = string.IsNullOrWhiteSpace(current.SourceAnimalName) ? prior.SourceAnimalName : current.SourceAnimalName;
        current.OfficialId = string.IsNullOrWhiteSpace(current.OfficialId) ? prior.OfficialId : current.OfficialId;
        current.DaysInMilk ??= prior.DaysInMilk; current.Milk ??= prior.Milk; current.FatPercent ??= prior.FatPercent;
        current.ProteinPercent ??= prior.ProteinPercent; current.LastCalvingDate ??= prior.LastCalvingDate;
        current.Tpi ??= prior.Tpi; current.NetMerit ??= prior.NetMerit; current.MilkPta ??= prior.MilkPta;
        current.FatPta ??= prior.FatPta; current.ProteinPta ??= prior.ProteinPta; current.SomaticCellScore ??= prior.SomaticCellScore;
        current.DaughterPregnancyRate ??= prior.DaughterPregnancyRate; current.ProductiveLife ??= prior.ProductiveLife;
        current.TypeScore ??= prior.TypeScore; current.UdderComposite ??= prior.UdderComposite;
        current.FeetLegsComposite ??= prior.FeetLegsComposite;

        var merged = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            foreach (var item in JsonSerializer.Deserialize<Dictionary<string, string>>(prior.RawDataJson) ?? []) merged[item.Key] = item.Value;
        }
        catch (JsonException) { }
        try
        {
            foreach (var item in JsonSerializer.Deserialize<Dictionary<string, string>>(current.RawDataJson) ?? [])
                if (!string.IsNullOrWhiteSpace(item.Value)) merged[item.Key] = item.Value;
        }
        catch (JsonException) { }
        current.RawDataJson = JsonSerializer.Serialize(merged);
    }

    private async Task EnrichConfirmedAnimalAsync(Animal animal, ParsedRow row, HerdDataSource source, CancellationToken ct)
    {
        var official = NormalizeId(row.OfficialId);
        var sourceId = NormalizeId(row.SourceAnimalId);
        var bestIdentifier = official.Length >= 9 ? official : sourceId.Length >= 9 ? sourceId : "";
        if (bestIdentifier.StartsWith("HO", StringComparison.Ordinal) && bestIdentifier[2..].All(char.IsDigit))
            bestIdentifier = bestIdentifier[2..];

        var changed = false;
        if (string.IsNullOrWhiteSpace(animal.RegistrationNumber) && bestIdentifier.Length >= 9)
        {
            animal.RegistrationNumber = bestIdentifier[..Math.Min(bestIdentifier.Length, 100)];
            changed = true;
        }
        if (source == HerdDataSource.Zoetis && string.IsNullOrWhiteSpace(animal.RegisteredName) && !string.IsNullOrWhiteSpace(row.SourceName))
        {
            animal.RegisteredName = row.SourceName.Trim()[..Math.Min(row.SourceName.Trim().Length, 200)];
            changed = true;
        }
        if (source == HerdDataSource.Zoetis)
        {
            changed |= ApplyImportedPedigree(animal, "sire", row.ImportedSire, value => animal.SireName = value);
            if (!string.IsNullOrWhiteSpace(row.ImportedDam)
                && !SameText(animal.DamName, row.ImportedDam))
            {
                var oldDam = animal.DamName;
                animal.DamName = row.ImportedDam.Trim();
                animal.DamId = await ResolveUniqueAnimalIdAsync(row.ImportedDam, animal.AnimalId, ct);
                AddPedigreeCorrectionNote(animal, "dam", oldDam, animal.DamName);
                changed = true;
            }
        }
        if (changed)
        {
            animal.UpdatedAt = DateTime.UtcNow;
            animal.UpdatedBy = $"Confirmed {source} import";
        }
    }

    private bool ApplyImportedPedigree(Animal animal, string field, string? imported, Action<string> apply)
    {
        if (string.IsNullOrWhiteSpace(imported)) return false;
        var current = field == "sire" ? animal.SireName : animal.DamName;
        if (SameText(current, imported)) return false;
        var clean = imported.Trim();
        apply(clean);
        AddPedigreeCorrectionNote(animal, field, current, clean);
        return true;
    }

    private void AddPedigreeCorrectionNote(Animal animal, string field, string? oldValue, string newValue)
    {
        context.AnimalNotes.Add(new AnimalNote
        {
            AnimalId = animal.AnimalId,
            NoteDate = DateTime.UtcNow,
            NoteType = NoteType.Other,
            NoteText = $"[ZOETIS AUDIT] {field.ToUpperInvariant()} corrected from '{oldValue ?? "blank"}' to '{newValue}' using the confirmed genomic import. Original value retained in this audit note.",
            CreatedBy = "Confirmed Zoetis import"
        });
    }

    private async Task<int?> ResolveUniqueAnimalIdAsync(string name, int excludedAnimalId, CancellationToken ct)
    {
        var candidates = await context.Animals.AsNoTracking()
            .Where(candidate => candidate.AnimalId != excludedAnimalId
                && (candidate.BarnName == name || candidate.RegisteredName == name))
            .Select(candidate => candidate.AnimalId)
            .Take(2)
            .ToListAsync(ct);
        return candidates.Count == 1 ? candidates[0] : null;
    }

    private static void AddPedigreeAudit(HerdDataPreviewRow preview, ParsedRow row, Animal? animal, HerdDataSource source)
    {
        if (source != HerdDataSource.Zoetis || animal == null) return;
        if (!string.IsNullOrWhiteSpace(row.ImportedSire) && !SameText(animal.SireName, row.ImportedSire))
            preview.AuditWarnings.Add($"Sire conflict: app '{animal.SireName ?? "blank"}' / Zoetis '{row.ImportedSire}'. Confirmed import will use Zoetis and preserve the old value in an audit note.");
        if (!string.IsNullOrWhiteSpace(row.ImportedDam) && !SameText(animal.DamName, row.ImportedDam))
            preview.AuditWarnings.Add($"Dam conflict: app '{animal.DamName ?? "blank"}' / Zoetis '{row.ImportedDam}'. Confirmed import will use Zoetis and preserve the old value in an audit note.");
    }

    private static bool SameText(string? left, string? right) => Normalize(left) == Normalize(right);

    private static void AddDryCowAudit(HerdDataPreviewRow preview, ParsedRow row, Animal? animal, List<DryOffEvent> dryOffs, DateOnly reportDate)
    {
        if (!row.Values.ContainsKey("DryDate")) return;

        preview.ImportedDryDate = Date(row.Values.GetValueOrDefault("DryDate"));
        preview.ImportedLactation = Int(row.Values.GetValueOrDefault("Lactation"));
        preview.ReportedDaysDry = Int(row.Values.GetValueOrDefault("DaysDry"));
        if (!string.Equals(row.Values.GetValueOrDefault("DryStatus"), "3", StringComparison.OrdinalIgnoreCase))
            preview.AuditWarnings.Add("PC-DART row is not marked with DRY status code 3.");
        if (!preview.ImportedDryDate.HasValue)
        {
            preview.AuditWarnings.Add("No valid dry date was found; no dry-off event will be created.");
            return;
        }

        var calculated = reportDate.DayNumber - preview.ImportedDryDate.Value.DayNumber;
        if (preview.ReportedDaysDry.HasValue && Math.Abs(calculated - preview.ReportedDaysDry.Value) > 1)
            preview.AuditWarnings.Add($"Date calculates {calculated} days dry, but PC-DART reports {preview.ReportedDaysDry}.");
        if (animal == null) return;
        if (preview.ImportedLactation.HasValue && animal.CurrentLactation.HasValue && animal.CurrentLactation != preview.ImportedLactation)
            preview.AuditWarnings.Add($"App lactation {animal.CurrentLactation} differs from PC-DART lactation {preview.ImportedLactation}; the app value will be preserved for review.");

        var latest = dryOffs.Where(value => value.AnimalId == animal.AnimalId).OrderByDescending(value => value.DryOffDate).FirstOrDefault();
        if (latest != null && DateOnly.FromDateTime(latest.DryOffDate) != preview.ImportedDryDate)
        {
            var latestDate = DateOnly.FromDateTime(latest.DryOffDate);
            preview.AuditWarnings.Add(latestDate > preview.ImportedDryDate
                ? $"App has a newer dry-off date ({latestDate:MM/dd/yyyy}); the older PC-DART date will not replace it."
                : $"A prior dry-off ({latestDate:MM/dd/yyyy}) is retained; this report will add the newer dry period.");
        }
    }

    private async Task ApplyDryCowDataAsync(Animal animal, ParsedRow row, DateOnly reportDate, CancellationToken ct)
    {
        if (!row.Values.ContainsKey("DryDate")) return;
        var dryDate = Date(row.Values.GetValueOrDefault("DryDate"));
        if (!dryDate.HasValue || row.Values.GetValueOrDefault("DryStatus") != "3") return;
        var daysDry = Int(row.Values.GetValueOrDefault("DaysDry"));
        var calculatedDays = reportDate.DayNumber - dryDate.Value.DayNumber;
        if (daysDry.HasValue && Math.Abs(calculatedDays - daysDry.Value) > 1)
            throw new InvalidOperationException($"{row.SourceName}: dry date calculates {calculatedDays} days, but PC-DART reports {daysDry}. Nothing was applied; review the report date.");

        var importedLactation = Int(row.Values.GetValueOrDefault("Lactation"));
        var changed = false;
        if (!animal.CurrentLactation.HasValue && importedLactation.HasValue)
        {
            animal.CurrentLactation = importedLactation;
            changed = true;
        }
        if (animal.AnimalStatus == AnimalStatus.Active && animal.AnimalStage != AnimalStage.Dry)
        {
            animal.AnimalStage = AnimalStage.Dry;
            changed = true;
        }

        var existingDates = await context.DryOffEvents.Where(value => value.AnimalId == animal.AnimalId).ToListAsync(ct);
        var importedDateTime = dryDate.Value.ToDateTime(new TimeOnly(12, 0));
        var exactExists = existingDates.Any(value => DateOnly.FromDateTime(value.DryOffDate) == dryDate);
        var latestDate = existingDates.Count == 0 ? (DateOnly?)null : existingDates.Max(value => DateOnly.FromDateTime(value.DryOffDate));
        if (!exactExists && (!latestDate.HasValue || latestDate.Value < dryDate.Value))
        {
            context.DryOffEvents.Add(new DryOffEvent
            {
                AnimalId = animal.AnimalId,
                DryOffDate = importedDateTime,
                Reason = "PC-DART dry cow import",
                Notes = $"Report {reportDate:MM/dd/yyyy}; PC-DART lactation {importedLactation?.ToString() ?? "not supplied"}; reported {daysDry?.ToString() ?? "unknown"} days dry.",
                CreatedBy = "PC-DART 024 import",
                UpdatedBy = "PC-DART 024 import"
            });
        }
        if (changed)
        {
            animal.UpdatedAt = DateTime.UtcNow;
            animal.UpdatedBy = "PC-DART 024 import";
        }
    }

    private static List<ParsedRow> Parse(HerdDataImportRequest request)
    {
        var rows = ParseCsv(request.CsvText);
        if (rows.Count < 2) return [];
        var headers = rows[0];
        return rows.Skip(1).Where(r => r.Any(v => !string.IsNullOrWhiteSpace(v))).Select(values => ParsedRow.From(headers, values, request.Source)).ToList();
    }

    private static List<Animal> FindCandidates(ParsedRow row, List<Animal> animals)
    {
        var official = NormalizeId(row.OfficialId);
        var sourceName = Normalize(row.SourceName);
        var sourceId = NormalizeId(row.SourceAnimalId);
        var scored = animals.Select(animal => new
        {
            Animal = animal,
            Score = CandidateScore(animal, official, sourceId, sourceName)
        }).Where(x => x.Score > 0).ToList();

        if (scored.Count == 0) return [];
        var bestScore = scored.Max(x => x.Score);
        return scored.Where(x => x.Score == bestScore).Select(x => x.Animal).DistinctBy(a => a.AnimalId).ToList();
    }

    private static int CandidateScore(Animal animal, string official, string sourceId, string sourceName)
    {
        if (RegistrationMatch(official, animal.RegistrationNumber) || RegistrationMatch(sourceId, animal.RegistrationNumber)) return 100;
        if (string.IsNullOrEmpty(sourceName)) return 0;

        var barnName = Normalize(animal.BarnName);
        var registeredName = Normalize(animal.RegisteredName);
        if ((!string.IsNullOrEmpty(barnName) && barnName == sourceName)
            || (!string.IsNullOrEmpty(registeredName) && registeredName == sourceName)) return 80;
        if ((!string.IsNullOrEmpty(barnName) && (barnName.StartsWith(sourceName) || sourceName.StartsWith(barnName)))
            || (!string.IsNullOrEmpty(registeredName) && (registeredName.StartsWith(sourceName) || sourceName.StartsWith(registeredName)))) return 60;
        return 0;
    }

    private static bool RegistrationMatch(string source, string? target)
    {
        var normalized = NormalizeId(target);
        return source.Length >= 6 && normalized.Length >= 6 && (source == normalized || source.EndsWith(normalized) || normalized.EndsWith(source));
    }
    private static string Normalize(string? value) => new((value ?? "").ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
    private static string NormalizeId(string? value) => new((value ?? "").Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());
    private static string ImportBucket(string fileName)
    {
        var separator = fileName.IndexOf("::", StringComparison.Ordinal);
        return separator > 0 ? fileName[..separator].ToUpperInvariant() : "CSV";
    }
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private static decimal? Dec(string? value) => decimal.TryParse(value?.Replace(",", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
    private static int? Int(string? value) => int.TryParse(value?.Replace(",", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
    private static DateOnly? Date(string? value) => DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed) ? parsed : null;

    private static List<List<string>> ParseCsv(string text)
    {
        var result = new List<List<string>>(); var row = new List<string>(); var field = new StringBuilder(); var quoted = false;
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c == '"') { if (quoted && i + 1 < text.Length && text[i + 1] == '"') { field.Append('"'); i++; } else quoted = !quoted; }
            else if (c == ',' && !quoted) { row.Add(field.ToString()); field.Clear(); }
            else if ((c == '\n' || c == '\r') && !quoted) { if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++; row.Add(field.ToString()); field.Clear(); if (row.Any(v => v.Length > 0)) result.Add(row); row = []; }
            else field.Append(c);
        }
        row.Add(field.ToString()); if (row.Any(v => v.Length > 0)) result.Add(row); return result;
    }

    private sealed class ParsedRow
    {
        public string SourceKey { get; init; } = ""; public string SourceName { get; init; } = ""; public string SourceAnimalId { get; init; } = ""; public string? OfficialId { get; init; }
        public DateOnly? BirthDate { get; init; } public string? Breed { get; init; } public string? ImportedSex { get; init; }
        public string? ImportedSire { get; init; } public string? ImportedDam { get; init; }
        public Dictionary<string, string> Values { get; init; } = [];
        public static ParsedRow From(List<string> headers, List<string> values, HerdDataSource source)
        {
            var data = headers.Select((h, i) => new { Key = h.Trim(), Value = i < values.Count ? values[i].Trim() : "" }).GroupBy(x => x.Key).ToDictionary(g => g.Key, g => g.Last().Value, StringComparer.OrdinalIgnoreCase);
            var id = source == HerdDataSource.Pcdart ? data.GetValueOrDefault("DHIID", "") : data.GetValueOrDefault("Animal ID", "");
            var name = source == HerdDataSource.Pcdart ? data.GetValueOrDefault("BarnName", "") : data.GetValueOrDefault("Animal Name", "");
            var official = source == HerdDataSource.Pcdart
                ? data.GetValueOrDefault("DHIID")
                : FirstValue(data, "Official ID", "CDCB #", "Registration Number", "Reg #");
            return new ParsedRow
            {
                SourceKey = NormalizeId(!string.IsNullOrWhiteSpace(official) ? official : !string.IsNullOrWhiteSpace(id) ? id : name),
                SourceName = name, SourceAnimalId = id, OfficialId = official,
                BirthDate = Date(data.GetValueOrDefault("Birth Date") ?? data.GetValueOrDefault("BirthDate")),
                Breed = data.GetValueOrDefault("Breed"), ImportedSex = data.GetValueOrDefault("Sex"),
                ImportedSire = FirstValue(data, "Sire Name", "Sire", "Sire Short Name", "Sire NAAB"),
                ImportedDam = FirstValue(data, "Dam Name", "Dam", "Dam Short Name", "Dam ID"),
                Values = data
            };
        }
        private static string? FirstValue(Dictionary<string, string> values, params string[] aliases) =>
            aliases.Select(alias => values.GetValueOrDefault(alias))
                .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
        public AnimalDataRecord ToRecord(int animalId, DateOnly reportDate, HerdDataSource source) => new()
        {
            AnimalId = animalId, Source = source, ReportDate = reportDate, SourceAnimalId = SourceAnimalId, SourceAnimalName = SourceName, OfficialId = OfficialId,
            DaysInMilk = Int(Values.GetValueOrDefault("DIM")), Milk = source == HerdDataSource.Pcdart ? Dec(Values.GetValueOrDefault("Milk")) : null,
            FatPercent = Dec(First("Fat%", "Fat %", "Curr TD % Fat", "Current TD % Fat")), ProteinPercent = Dec(First("Pro%", "Protein%", "Protein %", "Prt%", "Curr TD % Prt", "Current TD % Prt")), LastCalvingDate = Date(Values.GetValueOrDefault("LastCalv")),
            Tpi = Int(Values.GetValueOrDefault("TPI")), NetMerit = Int(Values.GetValueOrDefault("NM$")), MilkPta = source == HerdDataSource.Zoetis ? Int(Values.GetValueOrDefault("MILK")) : null,
            FatPta = Int(Values.GetValueOrDefault("FAT")), ProteinPta = Int(Values.GetValueOrDefault("PROT")), SomaticCellScore = Dec(First("SCS", "SCC", "Current SCC", "Curr SCC")),
            DaughterPregnancyRate = Dec(Values.GetValueOrDefault("DPR")), ProductiveLife = Dec(Values.GetValueOrDefault("PL")), TypeScore = Dec(Values.GetValueOrDefault("TYPE FS")),
            UdderComposite = Dec(Values.GetValueOrDefault("UDC")), FeetLegsComposite = Dec(Values.GetValueOrDefault("FLC")), RawDataJson = JsonSerializer.Serialize(Values)
        };
        private string? First(params string[] aliases) => aliases.Select(alias => Values.GetValueOrDefault(alias)).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
    }
}
