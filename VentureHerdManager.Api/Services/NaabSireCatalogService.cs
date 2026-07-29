using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic.FileIO;
using VentureHerdManager.Api.Data;
using VentureHerdManager.Api.Models;

namespace VentureHerdManager.Api.Services;

public sealed class NaabSireCatalogService
{
    private const int MinimumAissColumns = 95;
    private readonly ApplicationDbContext _context;

    public NaabSireCatalogService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<NaabImportResult> ImportAsync(
        Stream stream,
        string sourceFileName,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var result = new NaabImportResult();
        var existing = (await _context.SireReferences
                .ToListAsync(cancellationToken))
            .ToDictionary(
                sire => sire.ImportKey,
                StringComparer.OrdinalIgnoreCase);

        await using var transaction = _context.Database.IsRelational()
            ? await _context.Database.BeginTransactionAsync(cancellationToken)
            : null;
        using var parser = new TextFieldParser(
            stream,
            Encoding.UTF8,
            detectEncoding: true)
        {
            TextFieldType = FieldType.Delimited,
            HasFieldsEnclosedInQuotes = true,
            TrimWhiteSpace = false
        };
        parser.SetDelimiters(",");

        while (!parser.EndOfData)
        {
            cancellationToken.ThrowIfCancellationRequested();
            result.RowsRead++;

            string[]? fields;
            try
            {
                fields = parser.ReadFields();
            }
            catch (MalformedLineException exception)
            {
                AddError(
                    result,
                    $"Row {result.RowsRead}: malformed NAAB CSV ({exception.Message}).");
                continue;
            }

            if (fields == null || fields.All(string.IsNullOrWhiteSpace))
            {
                result.BlankRows++;
                continue;
            }

            if (fields.Length < MinimumAissColumns)
            {
                AddError(
                    result,
                    $"Row {result.RowsRead}: expected at least {MinimumAissColumns} AISS columns but found {fields.Length}.");
                continue;
            }

            var parsed = Parse(fields, sourceFileName);
            if (parsed == null)
            {
                AddError(
                    result,
                    $"Row {result.RowsRead}: missing both a sire identifier and name.");
                continue;
            }

            if (existing.TryGetValue(parsed.ImportKey, out var current))
            {
                if (string.Equals(
                        current.SourceRowHash,
                        parsed.SourceRowHash,
                        StringComparison.Ordinal))
                {
                    result.Unchanged++;
                    continue;
                }

                CopyValues(parsed, current);
                current.UpdatedAt = DateTime.UtcNow;
                result.Updated++;
            }
            else
            {
                _context.SireReferences.Add(parsed);
                existing.Add(parsed.ImportKey, parsed);
                result.Added++;
            }

            if ((result.Added + result.Updated) % 500 == 0)
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        if (transaction != null)
        {
            await transaction.CommitAsync(cancellationToken);
        }
        result.TotalCatalogRecords = existing.Count;
        return result;
    }

    private static SireReference? Parse(
        IReadOnlyList<string> fields,
        string sourceFileName)
    {
        var breed = Clean(fields[0]);
        var country = Clean(fields[1]);
        var registration = Clean(fields[2]);
        var naabCode = Clean(fields[7]);
        var name = Clean(fields[8]);
        var shortName = Clean(fields[90]);

        var importKey = NormalizeIdentifier(naabCode);
        if (string.IsNullOrEmpty(importKey))
        {
            importKey = NormalizeIdentifier(
                $"{breed}|{country}|{registration}");
        }

        if (string.IsNullOrEmpty(importKey)
            || (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(shortName)))
        {
            return null;
        }

        var now = DateTime.UtcNow;
        return new SireReference
        {
            ImportKey = importKey,
            BreedCode = breed,
            CountryCode = country,
            RegistrationNumber = registration,
            ControllerNumber = ParseInt(fields[3]),
            StudCode = ParseInt(fields[4]),
            NaabBreedCode = Clean(fields[5]),
            BullNumber = ParseInt(fields[6]),
            NaabCode = naabCode,
            Name = name ?? shortName!,
            ShortName = shortName,
            RegistryStatus = Clean(fields[9]),
            MarketingStatus = Clean(fields[94]),
            BirthDate = ParseDate(fields[89]),
            YieldReliability = ParseInt(fields[16]),
            PtaMilk = ParseInt(fields[18]),
            PtaFat = ParseInt(fields[19]),
            PtaFatPercent = ParseDecimal(fields[20]),
            PtaProtein = ParseInt(fields[21]),
            PtaProteinPercent = ParseDecimal(fields[22]),
            SomaticCellScore = ParseDecimal(fields[26]),
            ProductiveLife = ParseDecimal(fields[28]),
            DaughterPregnancyRate = ParseDecimal(fields[30]),
            HeiferConceptionRate = ParseDecimal(fields[35]),
            CowConceptionRate = ParseDecimal(fields[38]),
            Livability = ParseDecimal(fields[41]),
            NetMerit = ParseInt(fields[44]),
            SireCalvingEase = ParseDecimal(fields[47]),
            DaughterCalvingEase = ParseDecimal(fields[50]),
            PtaType = ParseDecimal(fields[60]),
            TotalPerformanceIndex = ParseInt(fields[62]),
            UdderComposite = ParseDecimal(fields[63]),
            FeetLegsComposite = ParseDecimal(fields[64]),
            SourceFileName = Path.GetFileName(sourceFileName)[..Math.Min(
                Path.GetFileName(sourceFileName).Length,
                260)],
            SourceRowHash = ComputeHash(fields),
            ImportedAt = now,
            UpdatedAt = now
        };
    }

    private static void CopyValues(
        SireReference source,
        SireReference destination)
    {
        destination.BreedCode = source.BreedCode;
        destination.CountryCode = source.CountryCode;
        destination.RegistrationNumber = source.RegistrationNumber;
        destination.ControllerNumber = source.ControllerNumber;
        destination.StudCode = source.StudCode;
        destination.NaabBreedCode = source.NaabBreedCode;
        destination.BullNumber = source.BullNumber;
        destination.NaabCode = source.NaabCode;
        destination.Name = source.Name;
        destination.ShortName = source.ShortName;
        destination.RegistryStatus = source.RegistryStatus;
        destination.MarketingStatus = source.MarketingStatus;
        destination.BirthDate = source.BirthDate;
        destination.YieldReliability = source.YieldReliability;
        destination.PtaMilk = source.PtaMilk;
        destination.PtaFat = source.PtaFat;
        destination.PtaFatPercent = source.PtaFatPercent;
        destination.PtaProtein = source.PtaProtein;
        destination.PtaProteinPercent = source.PtaProteinPercent;
        destination.SomaticCellScore = source.SomaticCellScore;
        destination.ProductiveLife = source.ProductiveLife;
        destination.DaughterPregnancyRate = source.DaughterPregnancyRate;
        destination.HeiferConceptionRate = source.HeiferConceptionRate;
        destination.CowConceptionRate = source.CowConceptionRate;
        destination.Livability = source.Livability;
        destination.NetMerit = source.NetMerit;
        destination.SireCalvingEase = source.SireCalvingEase;
        destination.DaughterCalvingEase = source.DaughterCalvingEase;
        destination.PtaType = source.PtaType;
        destination.TotalPerformanceIndex = source.TotalPerformanceIndex;
        destination.UdderComposite = source.UdderComposite;
        destination.FeetLegsComposite = source.FeetLegsComposite;
        destination.SourceFileName = source.SourceFileName;
        destination.SourceRowHash = source.SourceRowHash;
    }

    private static string? Clean(string? value)
    {
        var cleaned = value?.Trim();
        return string.IsNullOrWhiteSpace(cleaned) ? null : cleaned;
    }

    private static string NormalizeIdentifier(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return new string(
            value
                .Where(char.IsLetterOrDigit)
                .Select(char.ToUpperInvariant)
                .ToArray());
    }

    private static int? ParseInt(string? value) =>
        int.TryParse(
            value?.Trim(),
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var parsed)
            ? parsed
            : null;

    private static decimal? ParseDecimal(string? value) =>
        decimal.TryParse(
            value?.Trim(),
            NumberStyles.Number | NumberStyles.AllowLeadingSign,
            CultureInfo.InvariantCulture,
            out var parsed)
            ? parsed
            : null;

    private static DateOnly? ParseDate(string? value) =>
        DateOnly.TryParseExact(
            value?.Trim(),
            "yyyyMMdd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var parsed)
            ? parsed
            : null;

    private static string ComputeHash(IReadOnlyList<string> fields)
    {
        var canonical = string.Join(
            '\u001f',
            fields.Select(field => field.Trim()));
        return Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    private static void AddError(
        NaabImportResult result,
        string message)
    {
        result.Errors++;
        if (result.ErrorMessages.Count < 25)
        {
            result.ErrorMessages.Add(message);
        }
    }
}

public sealed class NaabImportResult
{
    public int RowsRead { get; set; }

    public int Added { get; set; }

    public int Updated { get; set; }

    public int Unchanged { get; set; }

    public int BlankRows { get; set; }

    public int Errors { get; set; }

    public int TotalCatalogRecords { get; set; }

    public List<string> ErrorMessages { get; } = [];
}
