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
        var saved = await context.AnimalIdentityMappings.AsNoTracking().Where(m => m.Source == request.Source).ToDictionaryAsync(m => m.SourceKey, ct);
        var existingImport = await context.HerdDataImports.AsNoTracking().FirstOrDefaultAsync(i =>
            i.FileHash == hash || (i.Source == request.Source && i.ReportDate == request.ReportDate), ct);
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
            preview.Rows.Add(new HerdDataPreviewRow
            {
                SourceKey = row.SourceKey, SourceName = row.SourceName, OfficialId = row.OfficialId,
                BirthDate = row.BirthDate, Breed = row.Breed, ImportedSex = row.ImportedSex,
                AnimalId = animal?.AnimalId, AnimalName = animal?.DisplayName,
                NeedsConfirmation = animal == null,
                Candidates = candidates.Take(12).Select(a => new HerdDataCandidate { AnimalId = a.AnimalId, AnimalName = a.DisplayName, RegistrationNumber = a.RegistrationNumber }).ToList()
            });
        }
        return preview;
    }

    public async Task<HerdDataImport> ApplyAsync(HerdDataImportRequest request, CancellationToken ct = default)
    {
        var hash = Hash(request.CsvText);
        var existing = await context.HerdDataImports.Include(i => i.Records).SingleOrDefaultAsync(i => i.FileHash == hash, ct);
        if (existing != null) return existing;
        var sameDateImport = await context.HerdDataImports.Include(i => i.Records)
            .SingleOrDefaultAsync(i => i.Source == request.Source && i.ReportDate == request.ReportDate, ct);
        if (sameDateImport != null && !request.ConfirmDuplicateReplace)
            throw new InvalidOperationException($"A {request.Source} report for {request.ReportDate:yyyy-MM-dd} is already stored. Review the duplicate warning and explicitly accept replacement or decline it.");
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
            batch.Records.Add(record);
            var animal = await context.Animals.FindAsync([match.AnimalId.Value], ct);
            if (animal != null) EnrichConfirmedAnimal(animal, row, request.Source);
            var mapping = await context.AnimalIdentityMappings.SingleOrDefaultAsync(m => m.Source == request.Source && m.SourceKey == row.SourceKey, ct);
            if (mapping == null) context.AnimalIdentityMappings.Add(new AnimalIdentityMapping { Source = request.Source, SourceKey = row.SourceKey, SourceLabel = row.SourceName, AnimalId = match.AnimalId.Value });
            else { mapping.AnimalId = match.AnimalId.Value; mapping.SourceLabel = row.SourceName; mapping.ConfirmedAt = DateTime.UtcNow; }
        }
        batch.RowsImported = batch.Records.Count;
        await context.SaveChangesAsync(ct);
        return batch;
    }

    private static void EnrichConfirmedAnimal(Animal animal, ParsedRow row, HerdDataSource source)
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
        if (changed)
        {
            animal.UpdatedAt = DateTime.UtcNow;
            animal.UpdatedBy = $"Confirmed {source} import";
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
        public Dictionary<string, string> Values { get; init; } = [];
        public static ParsedRow From(List<string> headers, List<string> values, HerdDataSource source)
        {
            var data = headers.Select((h, i) => new { Key = h.Trim(), Value = i < values.Count ? values[i].Trim() : "" }).GroupBy(x => x.Key).ToDictionary(g => g.Key, g => g.Last().Value, StringComparer.OrdinalIgnoreCase);
            var id = source == HerdDataSource.Pcdart ? data.GetValueOrDefault("DHIID", "") : data.GetValueOrDefault("Animal ID", "");
            var name = source == HerdDataSource.Pcdart ? data.GetValueOrDefault("BarnName", "") : data.GetValueOrDefault("Animal Name", "");
            var official = source == HerdDataSource.Pcdart ? data.GetValueOrDefault("DHIID") : data.GetValueOrDefault("Official ID");
            return new ParsedRow
            {
                SourceKey = NormalizeId(!string.IsNullOrWhiteSpace(official) ? official : !string.IsNullOrWhiteSpace(id) ? id : name),
                SourceName = name, SourceAnimalId = id, OfficialId = official,
                BirthDate = Date(data.GetValueOrDefault("Birth Date") ?? data.GetValueOrDefault("BirthDate")),
                Breed = data.GetValueOrDefault("Breed"), ImportedSex = data.GetValueOrDefault("Sex"), Values = data
            };
        }
        public AnimalDataRecord ToRecord(int animalId, DateOnly reportDate, HerdDataSource source) => new()
        {
            AnimalId = animalId, Source = source, ReportDate = reportDate, SourceAnimalId = SourceAnimalId, SourceAnimalName = SourceName, OfficialId = OfficialId,
            DaysInMilk = Int(Values.GetValueOrDefault("DIM")), Milk = source == HerdDataSource.Pcdart ? Dec(Values.GetValueOrDefault("Milk")) : null,
            FatPercent = Dec(Values.GetValueOrDefault("Fat%")), ProteinPercent = Dec(Values.GetValueOrDefault("Pro%")), LastCalvingDate = Date(Values.GetValueOrDefault("LastCalv")),
            Tpi = Int(Values.GetValueOrDefault("TPI")), NetMerit = Int(Values.GetValueOrDefault("NM$")), MilkPta = source == HerdDataSource.Zoetis ? Int(Values.GetValueOrDefault("MILK")) : null,
            FatPta = Int(Values.GetValueOrDefault("FAT")), ProteinPta = Int(Values.GetValueOrDefault("PROT")), SomaticCellScore = Dec(Values.GetValueOrDefault("SCS")),
            DaughterPregnancyRate = Dec(Values.GetValueOrDefault("DPR")), ProductiveLife = Dec(Values.GetValueOrDefault("PL")), TypeScore = Dec(Values.GetValueOrDefault("TYPE FS")),
            UdderComposite = Dec(Values.GetValueOrDefault("UDC")), FeetLegsComposite = Dec(Values.GetValueOrDefault("FLC")), RawDataJson = JsonSerializer.Serialize(Values)
        };
    }
}
