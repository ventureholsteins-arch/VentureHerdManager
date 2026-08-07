using System.Text;
using VentureHerdManager.Api.Data;
using VentureHerdManager.Api.DTOs;
using VentureHerdManager.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace VentureHerdManager.Api.Services;

public sealed class PcdartImportService
{
    private readonly ApplicationDbContext _context;

    public PcdartImportService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PcdartImportResult> ImportAsync(
        PcdartImportRequest request,
        bool apply,
        CancellationToken cancellationToken = default)
    {
        var reportLabel = string.IsNullOrWhiteSpace(request.ReportLabel)
            ? "PCDART Monthly Report"
            : request.ReportLabel.Trim();

        var result = new PcdartImportResult
        {
            Applied = apply,
            ReportLabel = reportLabel
        };

        var rows = ParseRows(request.RawText);
        result.RowsRead = rows.Count;
        var reportBarnNames = rows
            .Select(row => Normalize(row.BarnName))
            .Where(name => name.Length > 0)
            .ToHashSet();

        var animals = await _context.Animals
            .ToListAsync(cancellationToken);

        var lookup = BuildAnimalLookup(animals);
        var stageAuditByAnimalId = new HashSet<int>();

        foreach (var row in rows)
        {
            var match = FindAnimal(lookup, row.BarnName);
            if (match.IsAmbiguous)
            {
                result.Conflicts.Add($"Animal '{row.BarnName}' matched more than one cow and was skipped.");
                continue;
            }

            Animal animal;
            if (match.Animal == null)
            {
                result.MissingAnimals.Add(row.BarnName);
                if (!apply)
                {
                    continue;
                }

                animal = new Animal
                {
                    BarnName = row.BarnName,
                    Sex = AnimalSex.Unknown,
                    AnimalStage = AnimalStage.Unknown,
                    AnimalStatus = AnimalStatus.Active,
                    CreatedBy = "PCDART monthly import",
                    UpdatedBy = "PCDART monthly import"
                };

                _context.Animals.Add(animal);
                animals.Add(animal);
                AddAnimalToLookup(lookup, animal);
                result.AnimalsCreated++;
            }
            else
            {
                animal = match.Animal;
                result.AnimalsMatched++;

                if (animal.AnimalStage != AnimalStage.Milking
                    && stageAuditByAnimalId.Add(animal.AnimalId))
                {
                    var label = GetAnimalLabel(animal);
                    result.Alerts.Add(new PcdartAuditAlert
                    {
                        Severity = "warning",
                        Code = "stage-not-milking",
                        AnimalId = animal.AnimalId,
                        AnimalLabel = label,
                        Message = $"Appears in milking report but current stage is {animal.AnimalStage}."
                    });

                    result.SuggestedChanges.Add(new PcdartSuggestedChange
                    {
                        Code = "set-stage-milking",
                        AnimalId = animal.AnimalId,
                        AnimalLabel = label,
                        ProposedAction = "Set stage to Milking",
                        CanAutoApply = true
                    });

                    if (apply && request.ApplySuggestedChanges)
                    {
                        animal.AnimalStage = AnimalStage.Milking;
                        animal.UpdatedAt = DateTime.UtcNow;
                        animal.UpdatedBy = "PCDART monthly import";
                        result.SuggestedChangesApplied++;
                    }
                }
            }

            if (!apply)
            {
                continue;
            }

            var noteText = BuildNoteText(reportLabel, row);
            var duplicateExists = await _context.AnimalNotes.AnyAsync(
                note => note.AnimalId == animal.AnimalId && note.NoteText == noteText,
                cancellationToken);

            if (duplicateExists)
            {
                result.DuplicateNotesSkipped++;
                continue;
            }

            _context.AnimalNotes.Add(new AnimalNote
            {
                Animal = animal,
                AnimalId = animal.AnimalId,
                NoteDate = DateTime.UtcNow,
                NoteText = noteText,
                CreatedBy = "PCDART monthly import",
                NoteType = NoteType.Other
            });

            animal.UpdatedAt = DateTime.UtcNow;
            animal.UpdatedBy = "PCDART monthly import";
            result.NotesCreated++;
        }

        foreach (var animal in animals.Where(animal =>
                     animal.AnimalStatus == AnimalStatus.Active
                     && (animal.AnimalStage == AnimalStage.Milking || animal.AnimalStage == AnimalStage.Dry)
                     && !string.IsNullOrWhiteSpace(animal.BarnName)))
        {
            var normalizedBarnName = Normalize(animal.BarnName);
            if (normalizedBarnName.Length == 0 || reportBarnNames.Contains(normalizedBarnName))
            {
                continue;
            }

            result.Alerts.Add(new PcdartAuditAlert
            {
                Severity = "warning",
                Code = "missing-from-report",
                AnimalId = animal.AnimalId,
                AnimalLabel = GetAnimalLabel(animal),
                Message = "Active milking/dry cow is missing from this month report. Review for sold, deceased, or status change."
            });

            result.SuggestedChanges.Add(new PcdartSuggestedChange
            {
                Code = "review-archive-status",
                AnimalId = animal.AnimalId,
                AnimalLabel = GetAnimalLabel(animal),
                ProposedAction = "Review archive status manually",
                CanAutoApply = false
            });
        }

        if (apply)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        return result;
    }

    private static string BuildNoteText(string reportLabel, PcdartRow row)
    {
        var text = new StringBuilder();
        text.AppendLine($"[PCDART] {reportLabel}");
        text.AppendLine($"Barn: {row.BarnName}");
        if (!string.IsNullOrWhiteSpace(row.AgeLabel))
        {
            text.AppendLine($"Age: {row.AgeLabel}");
        }

        if (!string.IsNullOrWhiteSpace(row.DetailText))
        {
            text.AppendLine($"Details: {row.DetailText}");
        }

        text.Append($"Raw: {row.RawLine}");
        return text.ToString();
    }

    private static List<PcdartRow> ParseRows(string rawText)
    {
        var rows = new List<PcdartRow>();

        foreach (var line in rawText.Replace("\r", string.Empty).Split('\n'))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                continue;
            }

            if (trimmed.StartsWith("***", StringComparison.Ordinal) ||
                trimmed.StartsWith("Printed", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("Ref:", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("Age ", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("Age", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("Barn", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var tokens = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length < 2 || !LooksLikeAge(tokens[0]))
            {
                continue;
            }

            rows.Add(new PcdartRow
            {
                AgeLabel = tokens[0],
                BarnName = tokens[1],
                DetailText = tokens.Length > 2
                    ? string.Join(' ', tokens.Skip(2))
                    : string.Empty,
                RawLine = trimmed
            });
        }

        return rows;
    }

    private static bool LooksLikeAge(string value) =>
        value.Length == 5 &&
        value[2] == '-' &&
        char.IsDigit(value[0]) &&
        char.IsDigit(value[1]) &&
        char.IsDigit(value[3]) &&
        char.IsDigit(value[4]);

    private static Dictionary<string, List<Animal>> BuildAnimalLookup(IEnumerable<Animal> animals)
    {
        var lookup = new Dictionary<string, List<Animal>>(StringComparer.OrdinalIgnoreCase);

        foreach (var animal in animals)
        {
            AddAnimalToLookup(lookup, animal);
        }

        return lookup;
    }

    private static void AddAnimalToLookup(IDictionary<string, List<Animal>> lookup, Animal animal)
    {
        foreach (var value in new[] { animal.BarnName, animal.RegisteredName, animal.RegistrationNumber }
                     .Where(value => !string.IsNullOrWhiteSpace(value))
                     .Select(Normalize)
                     .Distinct())
        {
            if (!lookup.TryGetValue(value, out var matches))
            {
                matches = [];
                lookup[value] = matches;
            }

            if (!matches.Contains(animal))
            {
                matches.Add(animal);
            }
        }
    }

    private static AnimalMatch FindAnimal(
        IReadOnlyDictionary<string, List<Animal>> lookup,
        string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new AnimalMatch(null, false);
        }

        if (!lookup.TryGetValue(Normalize(value), out var matches) || matches.Count == 0)
        {
            return new AnimalMatch(null, false);
        }

        return matches.Count == 1
            ? new AnimalMatch(matches[0], false)
            : new AnimalMatch(null, true);
    }

    private static string Normalize(string? value) =>
        new string((value ?? string.Empty)
            .Trim()
            .ToLowerInvariant()
            .Where(character => char.IsLetterOrDigit(character))
            .ToArray());

    private static string GetAnimalLabel(Animal animal) =>
        !string.IsNullOrWhiteSpace(animal.BarnName)
            ? animal.BarnName.Trim()
            : !string.IsNullOrWhiteSpace(animal.RegisteredName)
                ? animal.RegisteredName.Trim()
                : $"Animal #{animal.AnimalId}";

    private sealed record AnimalMatch(Animal? Animal, bool IsAmbiguous);

    private sealed class PcdartRow
    {
        public string AgeLabel { get; init; } = string.Empty;

        public string BarnName { get; init; } = string.Empty;

        public string DetailText { get; init; } = string.Empty;

        public string RawLine { get; init; } = string.Empty;
    }
}