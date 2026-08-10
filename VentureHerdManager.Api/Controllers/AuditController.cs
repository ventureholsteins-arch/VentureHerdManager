using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VentureHerdManager.Api.Data;
using VentureHerdManager.Api.Models;
using VentureHerdManager.Api.Services;

namespace VentureHerdManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuditController(ApplicationDbContext context, HerdDataAdminAccess admin) : ControllerBase
{
    private IActionResult? Guard() => admin.IsAuthorized(Request) ? null : Unauthorized("Admin access is required for herd auditing.");
    private static string Normal(string? value) => new((value ?? "").ToUpperInvariant().Where(char.IsLetterOrDigit).ToArray());

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var denied = Guard(); if (denied != null) return denied;
        var animals = await context.Animals.AsNoTracking().ToListAsync(ct);
        var dataIds = await context.AnimalDataRecords.AsNoTracking().Where(record => record.OfficialId != null)
            .Select(record => new { record.AnimalId, record.OfficialId }).ToListAsync(ct);
        var savedMappings = await context.AnimalIdentityMappings.AsNoTracking().Select(mapping => new { mapping.AnimalId, mapping.Source, mapping.SourceKey }).ToListAsync(ct);
        var animalFindings = new List<object>();
        for (var leftIndex = 0; leftIndex < animals.Count; leftIndex++)
        for (var rightIndex = leftIndex + 1; rightIndex < animals.Count; rightIndex++)
        {
            var left = animals[leftIndex]; var right = animals[rightIndex]; var reasons = new List<string>();
            var leftRegistration = Normal(left.RegistrationNumber); var rightRegistration = Normal(right.RegistrationNumber);
            if (leftRegistration.Length >= 6 && leftRegistration == rightRegistration) reasons.Add("Same registration/DHI ID");
            if (leftRegistration.Length >= 8 && rightRegistration.Length >= 8 && (leftRegistration.EndsWith(rightRegistration, StringComparison.Ordinal) || rightRegistration.EndsWith(leftRegistration, StringComparison.Ordinal))) reasons.Add("Registration/DHI IDs share the same ending");
            var leftNames = new[] { Normal(left.BarnName), Normal(left.RegisteredName) }.Where(value => value.Length > 1).ToHashSet();
            var rightNames = new[] { Normal(right.BarnName), Normal(right.RegisteredName) }.Where(value => value.Length > 1).ToHashSet();
            if (leftNames.Overlaps(rightNames)) reasons.Add("Same animal name");
            if (leftNames.Any(leftName => rightNames.Any(rightName => leftName.Length >= 5 && rightName.Length >= 5 && EditDistance(leftName, rightName) <= 1))) reasons.Add("Animal names differ by only one character");
            var leftBarn = Normal(left.BarnName); var rightBarn = Normal(right.BarnName);
            var leftRegistered = Normal(left.RegisteredName); var rightRegistered = Normal(right.RegisteredName);
            if ((leftBarn.Length >= 4 && rightRegistered.Contains(leftBarn, StringComparison.Ordinal))
                || (rightBarn.Length >= 4 && leftRegistered.Contains(rightBarn, StringComparison.Ordinal)))
                reasons.Add("Barn name appears in the other card's registered name");
            if (left.BirthDate.HasValue && left.BirthDate == right.BirthDate && (leftNames.Overlaps(rightNames) || Normal(left.DamName) == Normal(right.DamName))) reasons.Add("Same birth date and identity clues");
            var leftImportedIds = dataIds.Where(value => value.AnimalId == left.AnimalId).Select(value => Normal(value.OfficialId)).Where(value => value.Length >= 6).ToHashSet();
            var rightImportedIds = dataIds.Where(value => value.AnimalId == right.AnimalId).Select(value => Normal(value.OfficialId)).Where(value => value.Length >= 6).ToHashSet();
            if (leftImportedIds.Overlaps(rightImportedIds)) reasons.Add("Same imported official ID");
            var leftMappingIds = savedMappings.Where(value => value.AnimalId == left.AnimalId).Select(value => $"{value.Source}|{Normal(value.SourceKey)}").ToHashSet();
            var rightMappingIds = savedMappings.Where(value => value.AnimalId == right.AnimalId).Select(value => $"{value.Source}|{Normal(value.SourceKey)}").ToHashSet();
            if (leftMappingIds.Overlaps(rightMappingIds)) reasons.Add("Same saved PC-DART/Zoetis identity mapping");
            if (reasons.Count > 0) animalFindings.Add(new { left = AnimalCard(left), right = AnimalCard(right), reasons = reasons.Distinct() });
        }

        var eventFindings = new List<object>();
        AddNearEvents(eventFindings, "heat", await context.HeatEvents.AsNoTracking().Include(value => value.Animal).ToListAsync(ct), value => value.AnimalId, value => value.HeatDateTime, value => value.HeatEventId, value => value.Animal?.DisplayName ?? $"#{value.AnimalId}", value => $"strength {value.HeatStrength}", 12);
        AddNearEvents(eventFindings, "breeding", await context.BreedingEvents.AsNoTracking().Include(value => value.Animal).ToListAsync(ct), value => value.AnimalId, value => value.BreedingDate, value => value.BreedingEventId, value => value.Animal?.DisplayName ?? $"#{value.AnimalId}", value => $"sire {value.SireUsed}", 36);
        AddNearEvents(eventFindings, "calving", await context.CalvingEvents.AsNoTracking().Include(value => value.Animal).ToListAsync(ct), value => value.AnimalId, value => value.CalvingDate, value => value.CalvingEventId, value => value.Animal?.DisplayName ?? $"#{value.AnimalId}", value => $"{value.NumberOfCalves} calf/calves", 48);
        AddNearEvents(eventFindings, "dryOff", await context.DryOffEvents.AsNoTracking().Include(value => value.Animal).ToListAsync(ct), value => value.AnimalId, value => value.DryOffDate, value => value.DryOffEventId, value => value.Animal?.DisplayName ?? $"#{value.AnimalId}", value => value.Reason ?? "dry-off", 48);
        AddNearEvents(eventFindings, "lutalyse", await context.LutalyseEvents.AsNoTracking().Include(value => value.Animal).ToListAsync(ct), value => value.AnimalId, value => value.AdministrationDate, value => value.LutalyseEventId, value => value.Animal?.DisplayName ?? $"#{value.AnimalId}", value => "LUT injection", 24);
        AddNearEvents(eventFindings, "classification", await context.ClassificationRecords.AsNoTracking().Include(value => value.Animal).ToListAsync(ct), value => value.AnimalId, value => value.ClassificationDate ?? value.CreatedAt, value => value.ClassificationRecordId, value => value.Animal?.DisplayName ?? $"#{value.AnimalId}", value => $"score {value.Score}", 168);
        return Ok(new { generatedAt = DateTime.UtcNow, animalFindings, eventFindings });
    }

    [HttpPost("merge")]
    public async Task<IActionResult> Merge(MergeAnimalsRequest request, CancellationToken ct)
    {
        var denied = Guard(); if (denied != null) return denied;
        if (request.KeepAnimalId == request.RemoveAnimalId) return BadRequest("Choose two different animal cards.");
        var keep = await context.Animals.SingleOrDefaultAsync(value => value.AnimalId == request.KeepAnimalId, ct);
        var remove = await context.Animals.SingleOrDefaultAsync(value => value.AnimalId == request.RemoveAnimalId, ct);
        if (keep == null || remove == null) return NotFound("One of the animal cards no longer exists.");
        await using var transaction = await context.Database.BeginTransactionAsync(ct);
        keep.BarnName ??= remove.BarnName; keep.RegisteredName ??= remove.RegisteredName; keep.RegistrationNumber ??= remove.RegistrationNumber;
        keep.BirthDate ??= remove.BirthDate; keep.Breed ??= remove.Breed; keep.SireName ??= remove.SireName; keep.DamName ??= remove.DamName;
        keep.ProfilePictureUrl ??= remove.ProfilePictureUrl; keep.Notes = string.Join("\n", new[] { keep.Notes, remove.Notes }.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct());
        keep.IsFavorite |= remove.IsFavorite; keep.UpdatedAt = DateTime.UtcNow; keep.UpdatedBy = "Audit merge";
        foreach (var value in await context.Animals.Where(value => value.DamId == remove.AnimalId).ToListAsync(ct)) value.DamId = keep.AnimalId;
        foreach (var value in await context.Animals.Where(value => value.SireId == remove.AnimalId).ToListAsync(ct)) value.SireId = keep.AnimalId;
        foreach (var value in await context.HeatEvents.Where(value => value.AnimalId == remove.AnimalId).ToListAsync(ct)) value.AnimalId = keep.AnimalId;
        foreach (var value in await context.BreedingEvents.Where(value => value.AnimalId == remove.AnimalId).ToListAsync(ct)) value.AnimalId = keep.AnimalId;
        foreach (var value in await context.CalvingEvents.Where(value => value.AnimalId == remove.AnimalId).ToListAsync(ct)) value.AnimalId = keep.AnimalId;
        foreach (var value in await context.CalvingEvents.Where(value => value.CalfAnimalId == remove.AnimalId).ToListAsync(ct)) value.CalfAnimalId = keep.AnimalId;
        foreach (var value in await context.DryOffEvents.Where(value => value.AnimalId == remove.AnimalId).ToListAsync(ct)) value.AnimalId = keep.AnimalId;
        foreach (var value in await context.AnimalNotes.Where(value => value.AnimalId == remove.AnimalId).ToListAsync(ct)) value.AnimalId = keep.AnimalId;
        foreach (var value in await context.ClassificationRecords.Where(value => value.AnimalId == remove.AnimalId).ToListAsync(ct)) value.AnimalId = keep.AnimalId;
        foreach (var value in await context.LutalyseEvents.Where(value => value.AnimalId == remove.AnimalId).ToListAsync(ct)) value.AnimalId = keep.AnimalId;
        foreach (var value in await context.AnimalPhotos.Where(value => value.AnimalId == remove.AnimalId).ToListAsync(ct)) value.AnimalId = keep.AnimalId;
        foreach (var value in await context.ShowAchievements.Where(value => value.AnimalId == remove.AnimalId).ToListAsync(ct)) value.AnimalId = keep.AnimalId;
        foreach (var value in await context.EmbryoRecords.Where(value => value.RecipientAnimalId == remove.AnimalId).ToListAsync(ct)) value.RecipientAnimalId = keep.AnimalId;
        foreach (var value in await context.EmbryoRecords.Where(value => value.DonorAnimalId == remove.AnimalId).ToListAsync(ct)) value.DonorAnimalId = keep.AnimalId;
        foreach (var value in await context.AnimalDataRecords.Where(value => value.AnimalId == remove.AnimalId).ToListAsync(ct)) value.AnimalId = keep.AnimalId;
        foreach (var value in await context.AnimalIdentityMappings.Where(value => value.AnimalId == remove.AnimalId).ToListAsync(ct)) value.AnimalId = keep.AnimalId;
        context.Animals.Remove(remove); await context.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        return Ok(new { keptAnimalId = keep.AnimalId, removedAnimalId = remove.AnimalId });
    }

    [HttpDelete("event/{eventType}/{eventId:int}")]
    public async Task<IActionResult> RemoveEvent(string eventType, int eventId, CancellationToken ct)
    {
        var denied = Guard(); if (denied != null) return denied;
        object? record = eventType switch {
            "heat" => await context.HeatEvents.FindAsync([eventId], ct), "breeding" => await context.BreedingEvents.FindAsync([eventId], ct),
            "calving" => await context.CalvingEvents.FindAsync([eventId], ct), "dryOff" => await context.DryOffEvents.FindAsync([eventId], ct),
            "lutalyse" => await context.LutalyseEvents.FindAsync([eventId], ct), "classification" => await context.ClassificationRecords.FindAsync([eventId], ct), _ => null };
        if (record == null) return NotFound(); context.Remove(record); await context.SaveChangesAsync(ct); return NoContent();
    }

    private static object AnimalCard(Animal animal) => new { animal.AnimalId, animal.BarnName, animal.RegisteredName, animal.RegistrationNumber, animal.BirthDate, animal.DamName, animal.SireName, animal.CreatedAt };
    private static void AddNearEvents<T>(List<object> output, string type, IEnumerable<T> values, Func<T, int> animalId, Func<T, DateTime> date, Func<T, int> id, Func<T, string> animalName, Func<T, string> detail, double maximumHours)
    {
        foreach (var group in values.GroupBy(animalId))
        {
            var records = group.OrderBy(date).ToList();
            for (var left = 0; left < records.Count; left++) for (var right = left + 1; right < records.Count; right++)
            {
                var hours = Math.Abs((date(records[right]) - date(records[left])).TotalHours); if (hours > maximumHours) break;
                var timing = hours < .02 ? "same timestamp" : hours < 24 ? $"{hours:0.#} hours apart" : $"{hours / 24:0.#} days apart";
                output.Add(new { eventType = type, animalId = group.Key, animalName = animalName(records[left]), signature = $"{date(records[left]):MMM d, yyyy h:mm tt} and {date(records[right]):MMM d, yyyy h:mm tt} - {timing} - {detail(records[left])} / {detail(records[right])}", eventIds = new[] { id(records[left]), id(records[right]) }, count = 2, reviewReason = $"Two {type} records are unusually close together" });
            }
        }
    }

    private static int EditDistance(string left, string right)
    {
        var prior = Enumerable.Range(0, right.Length + 1).ToArray();
        for (var i = 1; i <= left.Length; i++) { var current = new int[right.Length + 1]; current[0] = i; for (var j = 1; j <= right.Length; j++) current[j] = Math.Min(Math.Min(current[j - 1] + 1, prior[j] + 1), prior[j - 1] + (left[i - 1] == right[j - 1] ? 0 : 1)); prior = current; }
        return prior[right.Length];
    }
}

public sealed class MergeAnimalsRequest { public int KeepAnimalId { get; set; } public int RemoveAnimalId { get; set; } }
