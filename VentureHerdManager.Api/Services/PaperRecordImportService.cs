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
        var paperPregnantAnimalNames = animalRows
            .Where(row => IndicatesPregnant(
                row.Get("Stage / Status from Notes")))
            .Select(row => Normalize(row.Get("Animal Name")))
            .Where(name =>
                name.Length > 0
                && name != "pixie")
            .ToHashSet();
        var paperOpenAnimalNames = animalRows
            .Where(row => IndicatesOpen(row.Get("Stage / Status from Notes")))
            .Select(row => Normalize(row.Get("Animal Name")))
            .Where(name => name.Length > 0)
            .ToHashSet();
        var pregnancyConfirmations = new List<BreedingEvent>();

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

        foreach (var row in breedingRows)
        {
            var name = row.Get("Animal Name");
            if (Normalize(name) == "pixie"
                && !row.Get("Notes").Contains("Owner confirmed", StringComparison.OrdinalIgnoreCase))
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

            var duplicate = existingBreedings.FirstOrDefault(b =>
                b.AnimalId == match.Animal.AnimalId
                && b.BreedingDate.Date == bredDate.Date
                && Normalize(b.SireUsed) == Normalize(sire));
            if (duplicate != null)
            {
                if (IndicatesPregnant(row.Get("Paper Status")))
                {
                    pregnancyConfirmations.Add(duplicate);
                }
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
            if (breeding.PregnancyStatus == PregnancyStatus.Pregnant)
            {
                pregnancyConfirmations.Add(breeding);
            }
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
            var outcome = ParseEmbryoOutcome(row.Get("Outcome"));
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
                ApplyEmbryoOutcome(duplicate, linkedBreeding ?? existingBreedings.FirstOrDefault(
                    breeding => breeding.BreedingEventId == duplicate.BreedingEventId), outcome);
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
                await ReproductiveEventRules.ClosePriorServiceAsync(
                    _context,
                    recipient.AnimalId,
                    breedingDate,
                    "a paper-record embryo implant",
                    cancellationToken);
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

            var inventoryMatches = existingEmbryos.Where(e =>
                    e.RecipientAnimalId == null
                    && e.ImplantDate == null
                    && e.BreedingEventId == null
                    && (e.Status == EmbryoStatus.InStorage || e.Status == EmbryoStatus.Assigned)
                    && Normalize(e.Donor) == Normalize(donor)
                    && Normalize(e.Sire) == Normalize(sire))
                .ToList();
            EmbryoRecord embryo;
            if (inventoryMatches.Count > 0)
            {
                embryo = inventoryMatches
                    .OrderBy(e => e.EmbryoRecordId)
                    .First();
                embryo.DonorAnimalId ??= donorMatch.IsAmbiguous ? null : donorMatch.Animal?.AnimalId;
                embryo.Mating ??= mating;
                embryo.RecipientAnimalId = recipient.AnimalId;
                embryo.ImplantDate = implantDate;
                embryo.BreedingEventId = breeding.BreedingEventId;
                embryo.LinkedBreedingNote = $"Paper implant record linked to {recipient.DisplayName}.";
                embryo.Notes = AppendNote(embryo.Notes, row.Get("Review Note"));
                embryo.UpdatedBy = "Paper record import";
                embryo.UpdatedAt = DateTime.UtcNow;
                report.RecordsUpdated++;
            }
            else
            {
                embryo = new EmbryoRecord
                {
                    Donor = donor,
                    DonorAnimalId = donorMatch.IsAmbiguous ? null : donorMatch.Animal?.AnimalId,
                    Sire = sire,
                    Mating = mating,
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
            ApplyEmbryoOutcome(embryo, breeding, outcome);
        }

        foreach (var pregnantName in paperPregnantAnimalNames)
        {
            var confirmedAnimal = FindAnimal(
                animalLookup,
                pregnantName).Animal;
            if (confirmedAnimal == null)
            {
                continue;
            }

            var latestBreeding = existingBreedings
                .Where(b =>
                    b.AnimalId == confirmedAnimal.AnimalId)
                .OrderByDescending(b => b.BreedingDate)
                .ThenByDescending(b => b.BreedingEventId)
                .FirstOrDefault();
            if (latestBreeding == null)
            {
                report.Conflicts.Add(
                    $"Paper marks '{confirmedAnimal.DisplayName}' pregnant, but no reliable breeding/implant exists to confirm.");
                continue;
            }

            pregnancyConfirmations.Add(latestBreeding);
        }

        foreach (var openName in paperOpenAnimalNames)
        {
            var openAnimal = FindAnimal(animalLookup, openName).Animal;
            var latestBreeding = openAnimal == null
                ? null
                : existingBreedings
                    .Where(b => b.AnimalId == openAnimal.AnimalId)
                    .OrderByDescending(b => b.BreedingDate)
                    .ThenByDescending(b => b.BreedingEventId)
                    .FirstOrDefault();
            if (latestBreeding == null)
            {
                report.Conflicts.Add(
                    $"Paper marks '{openName}' open, but no breeding exists to update.");
                continue;
            }

            var changed = latestBreeding.PregnancyStatus != PregnancyStatus.Open;
            ReproductiveEventRules.ApplyPregnancyStatus(
                latestBreeding,
                PregnancyStatus.Open,
                latestBreeding.BreedingType == BreedingType.EmbryoTransfer,
                null);
            var linkedEmbryo = existingEmbryos.FirstOrDefault(e =>
                e.BreedingEventId == latestBreeding.BreedingEventId);
            if (linkedEmbryo != null)
            {
                changed |= linkedEmbryo.Status != EmbryoStatus.Failed;
                ReproductiveEventRules.SynchronizeEmbryoOutcome(
                    linkedEmbryo,
                    PregnancyStatus.Open);
            }
            if (changed)
            {
                report.RecordsUpdated++;
            }
        }

        foreach (var breeding in pregnancyConfirmations.Distinct())
        {
            var linkedEmbryo = existingEmbryos.FirstOrDefault(embryo =>
                embryo.BreedingEventId == breeding.BreedingEventId
                || (
                    embryo.RecipientAnimalId == breeding.AnimalId
                    && embryo.ImplantDate.HasValue
                    && embryo.ImplantDate.Value
                        .ToDateTime(TimeOnly.MinValue).Date
                        == breeding.BreedingDate.Date
                    && breeding.BreedingType
                        == BreedingType.EmbryoTransfer
                ));
            var isEmbryoTransfer =
                breeding.BreedingType == BreedingType.EmbryoTransfer
                || linkedEmbryo != null;
            var changed = breeding.PregnancyStatus
                != PregnancyStatus.Pregnant;
            var expectedDueDate = breeding.BreedingDate.AddDays(
                isEmbryoTransfer
                    ? ReproductiveEventRules
                        .EmbryoTransferGestationDays
                    : ReproductiveEventRules
                        .StandardGestationDays);
            changed |= breeding.ExpectedDueDate != expectedDueDate;

            ReproductiveEventRules.ApplyPregnancyStatus(
                breeding,
                PregnancyStatus.Pregnant,
                isEmbryoTransfer,
                breeding.PregnancyCheckDate);
            breeding.UpdatedBy = "Paper record import";

            if (linkedEmbryo != null)
            {
                changed |= linkedEmbryo.Status
                    != EmbryoStatus.Successful;
                ReproductiveEventRules.SynchronizeEmbryoOutcome(
                    linkedEmbryo,
                    PregnancyStatus.Pregnant);
            }

            if (changed)
            {
                report.RecordsUpdated++;
            }
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

    private static PregnancyStatus? ParseEmbryoOutcome(string? value)
    {
        var normalized = Normalize(value);
        if (normalized.Length == 0 || normalized is "unknown" or "unconfirmed")
        {
            return null;
        }
        if (normalized.Contains("did not stick") || normalized is "open" or "failed" or "not pregnant")
        {
            return PregnancyStatus.Open;
        }
        if (normalized.Contains("preg") || normalized is "successful" or "stuck")
        {
            return PregnancyStatus.Pregnant;
        }
        return null;
    }

    private static void ApplyEmbryoOutcome(
        EmbryoRecord embryo,
        BreedingEvent? breeding,
        PregnancyStatus? outcome)
    {
        if (outcome.HasValue && breeding != null)
        {
            ReproductiveEventRules.ApplyPregnancyStatus(
                breeding,
                outcome.Value,
                true,
                breeding.PregnancyCheckDate);
            ReproductiveEventRules.SynchronizeEmbryoOutcome(embryo, outcome.Value);
            breeding.UpdatedBy = "Paper record import";
        }
        else if (!outcome.HasValue)
        {
            embryo.Status = EmbryoStatus.Implanted;
        }
    }

    private static string? AppendNote(string? existing, string? added)
    {
        if (string.IsNullOrWhiteSpace(added)) return existing;
        if (string.IsNullOrWhiteSpace(existing)) return added.Trim();
        if (existing.Contains(added.Trim(), StringComparison.OrdinalIgnoreCase)) return existing;
        return $"{existing.Trim()} {added.Trim()}";
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
        var isEmbryoTransfer =
            type == BreedingType.EmbryoTransfer;
        var dueDate = bredDate.AddDays(
            isEmbryoTransfer
                ? ReproductiveEventRules.EmbryoTransferGestationDays
                : ReproductiveEventRules.StandardGestationDays);
        return new BreedingEvent
        {
            AnimalId = animalId,
            BreedingDate = bredDate,
            SireUsed = sire,
            BreedingType = type,
            PregnancyStatus = status,
            PregnancyCheckDueDate = bredDate.AddDays(
                isEmbryoTransfer
                    ? ReproductiveEventRules
                        .PregnancyCheckAfterTransferDays
                    : ReproductiveEventRules
                        .PregnancyCheckAfterBreedingDays),
            ExpectedDueDate = dueDate,
            RecommendedDryOffDate = dueDate.AddDays(
                -ReproductiveEventRules.DryPeriodDays),
            CloseUpDate = dueDate.AddDays(
                -ReproductiveEventRules.CloseUpDays),
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
        return normalized switch
        {
            "chaching" => "cha ching",
            "cinnabar" => "cinnabun",
            _ => normalized
        };
    }

    private static bool IndicatesPregnant(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && (
            value.Contains(
                "pregnant",
                StringComparison.OrdinalIgnoreCase)
            || value.Split(
                    [' ', '/', ',', ';', '-', '(', ')'],
                    StringSplitOptions.RemoveEmptyEntries)
                .Any(part => part.Equals(
                    "PG",
                    StringComparison.OrdinalIgnoreCase))
        );

    private static bool IndicatesOpen(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Contains("open", StringComparison.OrdinalIgnoreCase);

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
            "sea turtle" or "catalina" => AnimalStage.Milking,
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
