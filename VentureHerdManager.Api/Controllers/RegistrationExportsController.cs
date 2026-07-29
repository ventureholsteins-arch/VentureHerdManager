using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VentureHerdManager.Api.Data;
using VentureHerdManager.Api.Models;

namespace VentureHerdManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class RegistrationExportsController(
    ApplicationDbContext context) : ControllerBase
{
    [HttpGet("easy-id-prep.csv")]
    public async Task<IActionResult> ExportEasyIdPreparation(
        [FromQuery] bool onlyMissingRegistration = true,
        CancellationToken cancellationToken = default)
    {
        var query = context.Animals
            .AsNoTracking()
            .Where(animal =>
                animal.AnimalStatus == AnimalStatus.Active
                && (animal.AnimalStage == AnimalStage.Calf
                    || animal.AnimalStage == AnimalStage.Heifer));
        if (onlyMissingRegistration)
        {
            query = query.Where(animal =>
                animal.RegistrationNumber == null
                || animal.RegistrationNumber == "");
        }

        var animals = await query
            .OrderBy(animal => animal.BirthDate)
            .ThenBy(animal => animal.BarnName)
            .Select(animal => new
            {
                animal.AnimalId,
                animal.BarnName,
                animal.RegisteredName,
                animal.RegistrationNumber,
                animal.BirthDate,
                animal.Sex,
                animal.Breed,
                animal.SireName,
                animal.DamName,
                animal.DamId,
                DamRegistrationNumber = animal.Dam == null
                    ? null
                    : animal.Dam.RegistrationNumber
            })
            .ToListAsync(cancellationToken);

        var calfIds = animals.Select(animal => animal.AnimalId).ToArray();
        var calvingLinkRows = await context.CalvingEvents
            .AsNoTracking()
            .Where(calving =>
                calving.CalfAnimalId != null
                && calfIds.Contains(calving.CalfAnimalId.Value))
            .Select(calving => new
            {
                CalfAnimalId = calving.CalfAnimalId!.Value,
                calving.CalvingEventId,
                calving.AnimalId,
                DamBarnName = calving.Animal == null
                    ? null
                    : calving.Animal.BarnName,
                DamRegisteredName = calving.Animal == null
                    ? null
                    : calving.Animal.RegisteredName,
                DamRegistrationNumber = calving.Animal == null
                    ? null
                    : calving.Animal.RegistrationNumber
            })
            .ToListAsync(cancellationToken);
        var calvingLinks = calvingLinkRows
            .GroupBy(link => link.CalfAnimalId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(link => link.CalvingEventId)
                    .First());

        var csv = new StringBuilder();
        AppendRow(
            csv,
            "Animal ID",
            "Barn Name",
            "Registered Name",
            "Registration Number",
            "Birth Date",
            "Sex",
            "Breed",
            "Sire Name",
            "Dam Name",
            "Dam Registration Number",
            "Dam Herd Animal ID",
            "Calving Event ID",
            "ET Status",
            "Review Before EASY ID Submission");

        foreach (var animal in animals)
        {
            calvingLinks.TryGetValue(animal.AnimalId, out var calving);
            var damName = animal.DamName
                ?? calving?.DamBarnName
                ?? calving?.DamRegisteredName;
            var damRegistration = animal.DamRegistrationNumber
                ?? calving?.DamRegistrationNumber;
            var damId = animal.DamId ?? calving?.AnimalId;

            AppendRow(
                csv,
                animal.AnimalId.ToString(CultureInfo.InvariantCulture),
                animal.BarnName,
                animal.RegisteredName,
                animal.RegistrationNumber,
                animal.BirthDate?.ToString(
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture),
                animal.Sex switch
                {
                    AnimalSex.Female => "Female",
                    AnimalSex.Male => "Male",
                    _ => ""
                },
                animal.Breed,
                animal.SireName,
                damName,
                damRegistration,
                damId?.ToString(CultureInfo.InvariantCulture),
                calving?.CalvingEventId.ToString(
                    CultureInfo.InvariantCulture),
                "",
                BuildReviewNote(
                    animal.RegisteredName,
                    animal.BirthDate,
                    animal.Sex,
                    animal.SireName,
                    damName));
        }

        var bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true)
            .GetBytes(csv.ToString());
        var fileName =
            $"venture-easy-id-prep-{DateTime.UtcNow:yyyyMMdd}.csv";
        return File(bytes, "text/csv; charset=utf-8", fileName);
    }

    private static string BuildReviewNote(
        string? registeredName,
        DateOnly? birthDate,
        AnimalSex sex,
        string? sireName,
        string? damName)
    {
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(registeredName))
        {
            missing.Add("registered name");
        }

        if (birthDate == null)
        {
            missing.Add("birth date");
        }

        if (sex == AnimalSex.Unknown)
        {
            missing.Add("sex");
        }

        if (string.IsNullOrWhiteSpace(sireName))
        {
            missing.Add("sire");
        }

        if (string.IsNullOrWhiteSpace(damName))
        {
            missing.Add("dam");
        }

        var missingText = missing.Count == 0
            ? "Core fields present"
            : $"Missing: {string.Join(", ", missing)}";
        return $"{missingText}. Confirm official IDs, ET/twin status, ownership and Holstein EASY ID mapping before submission.";
    }

    private static void AppendRow(
        StringBuilder builder,
        params string?[] values)
    {
        builder.AppendLine(string.Join(
            ',',
            values.Select(EscapeCsv)));
    }

    private static string EscapeCsv(string? value)
    {
        var cleaned = value ?? string.Empty;
        return cleaned.IndexOfAny([',', '"', '\r', '\n']) >= 0
            ? $"\"{cleaned.Replace("\"", "\"\"")}\""
            : cleaned;
    }
}
