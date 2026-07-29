using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using VentureHerdManager.Api.Data;
using VentureHerdManager.Api.Models;

namespace VentureHerdManager.Api.Services;

public sealed class PaperRecordImportService
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public PaperRecordImportService(
        ApplicationDbContext context,
        IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    public async Task<PaperImportReport> ReconcileAsync(
        string? sourceDirectory,
        bool apply,
        CancellationToken cancellationToken = default)
    {
        var directory = ResolveDirectory(sourceDirectory);
        var animalRows = ReadCsv(Path.Combine(directory, "animals.csv"));
        var breedingRows = ReadCsv(Path.Combine(directory, "breedings.csv"));
        var embryoRows = ReadCsv(Path.Combine(directory, "embryos.csv"));
        var report = new PaperImportReport { Applied = apply };
        var paperRecipientNames = embryoRows
            .Select(row => Normalize(row.Get("Recipient / Linked Animal")))
            .Where(name => name.Length > 0)
            .ToHashSet();

        await using var transaction = _context.Database.IsRelational()
            ? await _context.Database.BeginTransactionAsync(cancellationToken)
            : null;

        var animals = await _context.Animals.ToListAsync(cancellationToken);
        var animalLookup = BuildAnimalLookup(animals);

        foreach (var row in animalRows)
        {
            var name = row.Get("Animal Name");
            if (string.IsNullOrWhiteSpace(name))
            {
                report.Conflicts.Add("animals.csv contains a row with no animal name.");
                continue;
            }

            if (Normalize(name) == "pixie")
            {
                report.IgnoredRows.Add(
                    "Pixie animal row was intentionally ignored for manual correction.");
                continue;
            }

            var match = FindAnimal(animalLookup, name);
            if (match.IsAmbiguous)
            {
                report.Conflicts.Add(
                    $"Animal '{name}' matches more than one existing animal; no record was changed.");
                continue;
            }

            if (match.Animal != null)
            {
                report.AnimalMatches++;
                PreservePaperNote(match.Animal, row);
                ApplyConfirmedAnimalState(match.Animal, name, report);
                continue;
            }

            var created = new Animal
            {
                BarnName = name.Trim(),
                Sex = paperRecipientNames.Contains(Normalize(name))
                    ? AnimalSex.Female
                    : AnimalSex.Unknown,
                AnimalStage = paperRecipientNames.Contains(Normalize(name))
                    ? AnimalStage.Heifer
                    : AnimalStage.Unknown,
                AnimalStatus = AnimalStatus.Active,
                Notes = BuildPaperNote(row),
                CreatedBy = "Paper record import",
                UpdatedBy = "Paper record import"
            };
            _context.Animals.Add(created);
            animals.Add(created);
            AddAnimalToLookup(animalLookup, created);
            report.MissingAnimals.Add(name.Trim());
            report.AnimalsCreated++;
            if (paperRecipientNames.Contains(Normalize(name)))
            {
                report.RecipientsCreated++;
            }
            ApplyConfirmedAnimalState(created, name, report);

        }

        await _context.SaveChangesAsync(cancellationToken);

        var existingBreedings = await _context.BreedingEvents
            .ToListAsync(cancellationToken);

        foreach (var confirmedPregnantName in new[] { "Casanova", "Missy", "Ernest" })
        {
            var confirmedAnimal = FindAnimal(animalLookup, confirmedPregnantName).Animal;
            if (confirmedAnimal == null)
            {
                continue;
            }
            var latestBreeding = existingBreedings
                .Where(b => b.AnimalId == confirmedAnimal.AnimalId)
                .OrderByDescending(b => b.BreedingDate)
                .ThenByDescending(b => b.BreedingEventId)
                .FirstOrDefault();
            if (latestBreeding != null
                && latestBreeding.PregnancyStatus != PregnancyStatus.Pregnant)
            {
                latestBreeding.PregnancyStatus = PregnancyStatus.Pregnant;
                latestBreeding.UpdatedBy = "Paper record import";
                latestBreeding.UpdatedAt = DateTime.UtcNow;
                report.RecordsUpdated++;
            }
        }

        foreach (var row in breedingRows)
        {
            var name = row.Get("Animal Name");
            if (Normalize(name) == "pixie")
            {
                report.IgnoredRows.Add(
                    "Pixie breeding row was intentionally ignored for manual correction.");
                continue;
            }
            var match = FindAnimal(animalLookup, name);
            if (match.IsAmbiguous || match.Animal == null)
            {
                report.Conflicts.Add(
                    $"Breeding for '{name}' could not be linked to one unambiguous animal.");
                continue;
            }

            if (!DateTime.TryParseExact(
                    row.Get("Bred Date"),
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeLocal,
                    out var bredDate))
            {
                report.Conflicts.Add(
                    $"Breeding for '{name}' has invalid bred date '{row.Get("Bred Date")}'.");
                continue;
            }

            var sire = row.Get("Sire / Mating").Trim();
            if (string.IsNullOrWhiteSpace(sire))
            {
                report.Conflicts.Add(
                    $"Breeding for '{name}' on {bredDate:yyyy-MM-dd} has no sire/mating.");
                continue;
            }

            var duplicate = existingBreedings.Any(b =>
                b.AnimalId == match.Animal.AnimalId
                && b.BreedingDate.Date == bredDate.Date
                && Normalize(b.SireUsed) == Normalize(sire));
            if (duplicate)
            {
                report.DuplicateBreedingsSkipped++;
                continue;
            }

            var paperStatus = row.Get("Paper Status");

            var breeding = CreateBreeding(
                match.Animal.AnimalId,
                bredDate,
                sire,
                BreedingType.Unknown,
                paperStatus.Contains("PG", StringComparison.OrdinalIgnoreCase)
                    ? PregnancyStatus.Pregnant
                    : PregnancyStatus.Unconfirmed,
                BuildBreedingNote(row));
            _context.BreedingEvents.Add(breeding);
            existingBreedings.Add(breeding);
            report.BreedingsAdded++;
        }

        await _context.SaveChangesAsync(cancellationToken);
        var existingEmbryos = await _context.EmbryoRecords.ToListAsync(cancellationToken);

        foreach (var row in embryoRows)
        {
            var recipientName = row.Get("Recipient / Linked Animal");
            var match = FindAnimal(animalLookup, recipientName);
            if (match.IsAmbiguous)
            {
                report.Conflicts.Add(
                    $"Embryo recipient '{recipientName}' is ambiguous; embryo was not created.");
                continue;
            }

            var recipient = match.Animal;
            if (recipient == null)
            {
                recipient = new Animal
                {
                    BarnName = recipientName.Trim(),
                    Sex = AnimalSex.Female,
                    AnimalStage = AnimalStage.Heifer,
                    AnimalStatus = AnimalStatus.Active,
                    Notes = "[Paper import] Created as an embryo recipient; other details were not present on paper.",
                    CreatedBy = "Paper record import",
                    UpdatedBy = "Paper record import"
                };
                _context.Animals.Add(recipient);
                await _context.SaveChangesAsync(cancellationToken);
                animals.Add(recipient);
                AddAnimalToLookup(animalLookup, recipient);
                report.MissingAnimals.Add(recipientName.Trim());
                report.AnimalsCreated++;
                report.RecipientsCreated++;
            }

            if (!DateOnly.TryParseExact(
                    row.Get("Implant / Bred Date"),
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var implantDate))
            {
                report.Conflicts.Add(
                    $"Embryo for '{recipientName}' has invalid implant date '{row.Get("Implant / Bred Date")}'.");
                continue;
            }

            var donor = row.Get("Embryo Dam").Trim();
            var sire = row.Get("Embryo Sire").Trim();
            var mating = row.Get("Mating").Trim();
            var duplicate = existingEmbryos.FirstOrDefault(e =>
                e.RecipientAnimalId == recipient.AnimalId
                && e.ImplantDate == implantDate
                && Normalize(e.Donor) == Normalize(donor)
                && Normalize(e.Sire) == Normalize(sire));
            if (duplicate != null)
            {
                duplicate.Mating ??= mating;
                var linkedBreeding = duplicate.BreedingEventId.HasValue
                    ? existingBreedings.FirstOrDefault(
                        breeding => breeding.BreedingEventId == duplicate.BreedingEventId.Value)
                    : null;
                var linkIsValid = linkedBreeding != null
                    && linkedBreeding.AnimalId == recipient.AnimalId
                    && linkedBreeding.BreedingDate.Date
                        == implantDate.ToDateTime(TimeOnly.MinValue).Date;
                if (!linkIsValid)
                {
                    report.Conflicts.Add(
                        $"Existing embryo {duplicate.EmbryoRecordId} for '{recipientName}' had a missing or inconsistent breeding link; a correct transfer history record was linked without deleting the old history.");
                    var repairedBreeding = existingBreedings.FirstOrDefault(b =>
                        b.AnimalId == recipient.AnimalId
                        && b.BreedingDate.Date == implantDate.ToDateTime(TimeOnly.MinValue).Date
                        && b.BreedingType == BreedingType.EmbryoTransfer
                        && Normalize(b.SireUsed) == Normalize(mating));
                    if (repairedBreeding == null)
                    {
                        repairedBreeding = CreateBreeding(
                            recipient.AnimalId,
                            implantDate.ToDateTime(TimeOnly.MinValue),
                            mating,
                            BreedingType.EmbryoTransfer,
                            PregnancyStatus.Unconfirmed,
                            $"Imported embryo transfer: {mating}. {row.Get("Review Note")}".Trim());
                        _context.BreedingEvents.Add(repairedBreeding);
                        existingBreedings.Add(repairedBreeding);
                        report.BreedingsAdded++;
                        await _context.SaveChangesAsync(cancellationToken);
                    }
                    duplicate.BreedingEventId = repairedBreeding.BreedingEventId;
                    duplicate.UpdatedBy = "Paper record import";
                    duplicate.UpdatedAt = DateTime.UtcNow;
                }
                report.DuplicateEmbryosSkipped++;
                continue;
            }

            var breedingDate = implantDate.ToDateTime(TimeOnly.MinValue);
            var breeding = existingBreedings.FirstOrDefault(b =>
                b.AnimalId == recipient.AnimalId
                && b.BreedingDate.Date == breedingDate.Date
                && b.BreedingType == BreedingType.EmbryoTransfer
                && Normalize(b.SireUsed) == Normalize(mating));
            if (breeding == null)
            {
                breeding = CreateBreeding(
                    recipient.AnimalId,
                    breedingDate,
                    mating,
                    BreedingType.EmbryoTransfer,
                    PregnancyStatus.Unconfirmed,
                    $"Imported embryo transfer: {mating}. {row.Get("Review Note")}".Trim());
                _context.BreedingEvents.Add(breeding);
                existingBreedings.Add(breeding);
                report.BreedingsAdded++;
                await _context.SaveChangesAsync(cancellationToken);
            }

            var donorMatch = FindAnimal(animalLookup, donor);
            if (donorMatch.IsAmbiguous)
            {
                report.Conflicts.Add(
                    $"Embryo donor '{donor}' is ambiguous; donor name was preserved without an animal link.");
            }

            var embryo = new EmbryoRecord
            {
                Donor = donor,
                DonorAnimalId = donorMatch.IsAmbiguous ? null : donorMatch.Animal?.AnimalId,
                Sire = sire,
                Mating = mating,
                Status = EmbryoStatus.Implanted,
                RecipientAnimalId = recipient.AnimalId,
                ImplantDate = implantDate,
                BreedingEventId = breeding.BreedingEventId,
                LinkedBreedingNote = $"Paper implant record linked to {recipient.DisplayName}.",
                Notes = row.Get("Review Note"),
                CreatedBy = "Paper record import",
                UpdatedBy = "Paper record import"
            };
            _context.EmbryoRecords.Add(embryo);
            existingEmbryos.Add(embryo);
            report.EmbryosAdded++;
        }

        await _context.SaveChangesAsync(cancellationToken);
        if (apply)
        {
            if (transaction != null)
            {
                await transaction.CommitAsync(cancellationToken);
            }
        }
        else
        {
            if (transaction != null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }
            _context.ChangeTracker.Clear();
        }

        return report;
    }

    private string ResolveDirectory(string? sourceDirectory)
    {
        if (!string.IsNullOrWhiteSpace(sourceDirectory))
        {
            return Path.GetFullPath(sourceDirectory);
        }

        var bundled = Path.Combine(_environment.ContentRootPath, "paper-record-import");
        if (Directory.Exists(bundled))
        {
            return bundled;
        }

        return Path.GetFullPath(Path.Combine(
            _environment.ContentRootPath,
            "..",
            "docs",
            "paper-record-import"));
    }

    private static BreedingEvent CreateBreeding(
        int animalId,
        DateTime bredDate,
        string sire,
        BreedingType type,
        PregnancyStatus status,
        string? notes)
    {
        var gestationStart = type == BreedingType.EmbryoTransfer
            ? bredDate.AddDays(-7)
            : bredDate;
        return new BreedingEvent
        {
            AnimalId = animalId,
            BreedingDate = bredDate,
            SireUsed = sire,
            BreedingType = type,
            PregnancyStatus = status,
            PregnancyCheckDueDate = bredDate.AddDays(type == BreedingType.EmbryoTransfer ? 28 : 30),
            ExpectedDueDate = gestationStart.AddDays(280),
            RecommendedDryOffDate = gestationStart.AddDays(220),
            CloseUpDate = gestationStart.AddDays(259),
            Notes = notes,
            CreatedBy = "Paper record import",
            UpdatedBy = "Paper record import"
        };
    }

    private static Dictionary<string, List<Animal>> BuildAnimalLookup(IEnumerable<Animal> animals)
    {
        var lookup = new Dictionary<string, List<Animal>>();
        foreach (var animal in animals)
        {
            AddAnimalToLookup(lookup, animal);
        }
        return lookup;
    }

    private static void AddAnimalToLookup(
        IDictionary<string, List<Animal>> lookup,
        Animal animal)
    {
        foreach (var name in new[] { animal.BarnName, animal.RegisteredName }
                     .Where(n => !string.IsNullOrWhiteSpace(n))
                     .Select(Normalize)
                     .Distinct())
        {
            if (!lookup.TryGetValue(name, out var matches))
            {
                matches = [];
                lookup[name] = matches;
            }
            if (!matches.Contains(animal))
            {
                matches.Add(animal);
            }
        }
    }

    private static AnimalMatch FindAnimal(
        IReadOnlyDictionary<string, List<Animal>> lookup,
        string? name)
    {
        if (string.IsNullOrWhiteSpace(name)
            || !lookup.TryGetValue(Normalize(name), out var matches))
        {
            return new AnimalMatch(null, false);
        }
        return matches.Count == 1
            ? new AnimalMatch(matches[0], false)
            : new AnimalMatch(null, true);
    }

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }
        var normalized = string.Join(
                ' ',
                value.Normalize(NormalizationForm.FormKC)
                    .Trim()
                    .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            .ToLowerInvariant();

        // Confirmed paper/database spelling variant. Keep aliases explicit so
        // unrelated animals are never joined by broad fuzzy matching.
        return normalized == "chaching" ? "cha ching" : normalized;
    }

    private static void PreservePaperNote(Animal animal, CsvRow row)
    {
        var note = BuildPaperNote(row);
        if (string.IsNullOrWhiteSpace(note)
            || animal.Notes?.Contains(note, StringComparison.Ordinal) == true)
        {
            return;
        }
        animal.Notes = string.IsNullOrWhiteSpace(animal.Notes)
            ? note
            : $"{animal.Notes}\n{note}";
        animal.UpdatedBy = "Paper record import";
        animal.UpdatedAt = DateTime.UtcNow;
    }

    private static void ApplyConfirmedAnimalState(
        Animal animal,
        string paperName,
        PaperImportReport report)
    {
        var normalizedName = Normalize(paperName);
        var confirmedStage = normalizedName switch
        {
            "missy" or "emmy" => AnimalStage.Dry,
            "sea turtle" => AnimalStage.Milking,
            _ => (AnimalStage?)null
        };
        if (!confirmedStage.HasValue
            || animal.AnimalStage == confirmedStage.Value)
        {
            return;
        }

        animal.AnimalStage = confirmedStage.Value;
        animal.UpdatedBy = "Paper record import";
        animal.UpdatedAt = DateTime.UtcNow;
        report.RecordsUpdated++;
    }

    private static string BuildPaperNote(CsvRow row) =>
        $"[Paper import] Status: {ValueOrUnknown(row.Get("Stage / Status from Notes"))}; Source: {ValueOrUnknown(row.Get("Source Note"))}.";

    private static string BuildBreedingNote(CsvRow row)
    {
        var parts = new[]
        {
            "Imported from paper breeding record.",
            string.IsNullOrWhiteSpace(row.Get("Paper Status")) ? null : $"Paper status: {row.Get("Paper Status")}.",
            string.IsNullOrWhiteSpace(row.Get("Notes")) ? null : row.Get("Notes")
        };
        return string.Join(" ", parts.Where(p => p != null));
    }

    private static string ValueOrUnknown(string value) =>
        string.IsNullOrWhiteSpace(value) ? "not stated" : value.Trim();

    private static List<CsvRow> ReadCsv(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Paper import source file was not found.", path);
        }
        var rows = ParseCsv(File.ReadAllText(path));
        if (rows.Count == 0)
        {
            return [];
        }
        var headers = rows[0];
        return rows.Skip(1)
            .Where(values => values.Any(v => !string.IsNullOrWhiteSpace(v)))
            .Select(values => new CsvRow(headers, values))
            .ToList();
    }

    internal static List<List<string>> ParseCsv(string text)
    {
        var rows = new List<List<string>>();
        var row = new List<string>();
        var field = new StringBuilder();
        var quoted = false;
        for (var index = 0; index < text.Length; index++)
        {
            var character = text[index];
            if (quoted)
            {
                if (character == '"' && index + 1 < text.Length && text[index + 1] == '"')
                {
                    field.Append('"');
                    index++;
                }
                else if (character == '"')
                {
                    quoted = false;
                }
                else
                {
                    field.Append(character);
                }
            }
            else if (character == '"')
            {
                quoted = true;
            }
            else if (character == ',')
            {
                row.Add(field.ToString());
                field.Clear();
            }
            else if (character is '\r' or '\n')
            {
                if (character == '\r' && index + 1 < text.Length && text[index + 1] == '\n')
                {
                    index++;
                }
                row.Add(field.ToString());
                field.Clear();
                rows.Add(row);
                row = [];
            }
            else
            {
                field.Append(character);
            }
        }
        if (field.Length > 0 || row.Count > 0)
        {
            row.Add(field.ToString());
            rows.Add(row);
        }
        return rows;
    }

    private sealed record AnimalMatch(Animal? Animal, bool IsAmbiguous);

    private sealed class CsvRow
    {
        private readonly Dictionary<string, string> _values;

        public CsvRow(IReadOnlyList<string> headers, IReadOnlyList<string> values)
        {
            _values = headers
                .Select((header, index) => new
                {
                    Header = header.TrimStart('\uFEFF').Trim(),
                    Value = index < values.Count ? values[index].Trim() : string.Empty
                })
                .ToDictionary(item => item.Header, item => item.Value, StringComparer.OrdinalIgnoreCase);
        }

        public string Get(string header) =>
            _values.TryGetValue(header, out var value) ? value : string.Empty;
    }
}

public sealed class PaperImportReport
{
    public bool Applied { get; init; }
    public int AnimalMatches { get; set; }
    public int AnimalsCreated { get; set; }
    public int RecipientsCreated { get; set; }
    public int BreedingsAdded { get; set; }
    public int EmbryosAdded { get; set; }
    public int DuplicateBreedingsSkipped { get; set; }
    public int DuplicateEmbryosSkipped { get; set; }
    public int RecordsUpdated { get; set; }
    public List<string> MissingAnimals { get; } = [];
    public List<string> Conflicts { get; } = [];
    public List<string> IgnoredRows { get; } = [];
}
