using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VentureHerdManager.Api.Data;
using VentureHerdManager.Api.Models;
using VentureHerdManager.Api.Services;

namespace VentureHerdManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class SiresController : ControllerBase
{
    private const long MaxImportBytes = 25 * 1024 * 1024;
    private readonly ApplicationDbContext _context;
    private readonly NaabSireCatalogService _catalogService;
    private readonly IConfiguration _configuration;

    public SiresController(
        ApplicationDbContext context,
        NaabSireCatalogService catalogService,
        IConfiguration configuration)
    {
        _context = context;
        _catalogService = catalogService;
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string? search,
        [FromQuery] int limit = 40,
        CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 100);
        var query = _context.SireReferences.AsNoTracking();
        var cleaned = search?.Trim();
        if (!string.IsNullOrEmpty(cleaned))
        {
            query = query.Where(sire =>
                sire.Name.Contains(cleaned)
                || (sire.ShortName != null
                    && sire.ShortName.Contains(cleaned))
                || (sire.NaabCode != null
                    && sire.NaabCode.Contains(cleaned))
                || (sire.RegistrationNumber != null
                    && sire.RegistrationNumber.Contains(cleaned)));
        }

        var totalCatalogRecords =
            await _context.SireReferences.CountAsync(cancellationToken);
        var matchEntities = await query
            .OrderBy(sire => sire.ShortName ?? sire.Name)
            .ThenBy(sire => sire.NaabCode)
            .Take(limit)
            .ToListAsync(cancellationToken);
        var matches = matchEntities.Select(ToDto).ToList();

        return Ok(new
        {
            TotalCatalogRecords = totalCatalogRecords,
            Matches = matches
        });
    }

    [HttpGet("used")]
    public async Task<IActionResult> GetUsedSires(
        CancellationToken cancellationToken)
    {
        var breedings = await _context.BreedingEvents
            .AsNoTracking()
            .Select(breeding => new
            {
                breeding.AnimalId,
                breeding.SireUsed,
                breeding.BreedingType,
                breeding.BreedingDate,
                breeding.PregnancyStatus
            })
            .ToListAsync(cancellationToken);
        var catalog = await _context.SireReferences
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var aliases = BuildAliasLookup(catalog);
        var usage = breedings
            .Select(breeding => new
            {
                Breeding = breeding,
                SireName = ExtractSireName(
                    breeding.SireUsed,
                    breeding.BreedingType)
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.SireName))
            .GroupBy(
                item => Normalize(item.SireName),
                StringComparer.Ordinal)
            .Select(group =>
            {
                var first = group.First();
                var matchKey = Normalize(first.SireName);
                aliases.TryGetValue(matchKey, out var matches);
                var catalogMatch = matches?.Count == 1
                    ? matches[0]
                    : null;
                return new
                {
                    Sire = first.SireName,
                    RecordedNames = group
                        .Select(item => item.Breeding.SireUsed.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(value => value)
                        .ToArray(),
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
                    Unconfirmed = group.Count(item =>
                        item.Breeding.PregnancyStatus
                            is PregnancyStatus.Unconfirmed
                            or PregnancyStatus.Recheck),
                    FirstUsed = group.Min(item =>
                        item.Breeding.BreedingDate),
                    LastUsed = group.Max(item =>
                        item.Breeding.BreedingDate),
                    CatalogMatch = catalogMatch == null
                        ? null
                        : ToDto(catalogMatch),
                    CatalogMatchStatus = matches switch
                    {
                        null => "Not found",
                        { Count: 1 } => "Matched",
                        _ => "Multiple NAAB matches"
                    }
                };
            })
            .OrderByDescending(item => item.LastUsed)
            .ThenByDescending(item => item.Breedings)
            .ThenBy(item => item.Sire)
            .ToList();

        return Ok(new
        {
            TotalBreedings = breedings.Count,
            UniqueSires = usage.Count,
            Sires = usage
        });
    }

    [HttpPost("import-naab")]
    [RequestSizeLimit(MaxImportBytes)]
    public async Task<IActionResult> ImportNaab(
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (!HasValidImportKey())
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new
                {
                    Message =
                        "NAAB catalog import is locked. Configure SireCatalog:ImportKey and send it in X-NAAB-Import-Key."
                });
        }

        if (file == null || file.Length == 0)
        {
            return BadRequest("Choose an official NAAB AISS text or CSV file.");
        }

        if (file.Length > MaxImportBytes)
        {
            return BadRequest("The NAAB import file cannot exceed 25 MB.");
        }

        var extension = Path.GetExtension(file.FileName);
        if (!extension.Equals(".txt", StringComparison.OrdinalIgnoreCase)
            && !extension.Equals(".csv", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("NAAB imports must be an AISS .txt or .csv file.");
        }

        await using var stream = file.OpenReadStream();
        var result = await _catalogService.ImportAsync(
            stream,
            file.FileName,
            cancellationToken);
        return Ok(result);
    }

    private bool HasValidImportKey()
    {
        var expected = _configuration["SireCatalog:ImportKey"];
        var supplied = Request.Headers["X-NAAB-Import-Key"].ToString();
        if (string.IsNullOrWhiteSpace(expected)
            || string.IsNullOrWhiteSpace(supplied))
        {
            return false;
        }

        var expectedHash =
            SHA256.HashData(Encoding.UTF8.GetBytes(expected));
        var suppliedHash =
            SHA256.HashData(Encoding.UTF8.GetBytes(supplied));
        return CryptographicOperations.FixedTimeEquals(
            expectedHash,
            suppliedHash);
    }

    private static Dictionary<string, List<SireReference>>
        BuildAliasLookup(IEnumerable<SireReference> catalog)
    {
        var result = new Dictionary<string, List<SireReference>>(
            StringComparer.Ordinal);
        foreach (var sire in catalog)
        {
            foreach (var alias in new[]
                     {
                         sire.Name,
                         sire.ShortName,
                         sire.NaabCode,
                         sire.RegistrationNumber
                     })
            {
                var key = Normalize(alias);
                if (key.Length == 0)
                {
                    continue;
                }

                if (!result.TryGetValue(key, out var matches))
                {
                    matches = [];
                    result.Add(key, matches);
                }

                if (!matches.Contains(sire))
                {
                    matches.Add(sire);
                }
            }
        }

        return result;
    }

    private static string ExtractSireName(
        string sireUsed,
        BreedingType breedingType)
    {
        var cleaned = sireUsed.Trim();
        var separators = new[] { " x ", " × " };
        foreach (var separator in separators)
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

    private static string Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : new string(
                value
                    .Where(char.IsLetterOrDigit)
                    .Select(char.ToUpperInvariant)
                    .ToArray());

    private static object ToDto(SireReference sire) => new
    {
        sire.SireReferenceId,
        sire.Name,
        sire.ShortName,
        sire.NaabCode,
        sire.RegistrationNumber,
        sire.BreedCode,
        sire.CountryCode,
        sire.RegistryStatus,
        sire.MarketingStatus,
        sire.BirthDate,
        sire.YieldReliability,
        sire.PtaMilk,
        sire.PtaFat,
        sire.PtaFatPercent,
        sire.PtaProtein,
        sire.PtaProteinPercent,
        sire.SomaticCellScore,
        sire.ProductiveLife,
        sire.DaughterPregnancyRate,
        sire.HeiferConceptionRate,
        sire.CowConceptionRate,
        sire.Livability,
        sire.NetMerit,
        sire.SireCalvingEase,
        sire.DaughterCalvingEase,
        sire.PtaType,
        sire.TotalPerformanceIndex,
        sire.UdderComposite,
        sire.FeetLegsComposite,
        sire.SourceFileName,
        sire.UpdatedAt
    };
}
