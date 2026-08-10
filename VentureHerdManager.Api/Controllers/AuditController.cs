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
        var animalFindings = new List<object>();
        for (var leftIndex = 0; leftIndex < animals.Count; leftIndex++)
        for (var rightIndex = leftIndex + 1; rightIndex < animals.Count; rightIndex++)
        {
            var left = animals[leftIndex]; var right = animals[rightIndex]; var reasons = new List<string>();
            var leftRegistration = Normal(left.RegistrationNumber); var rightRegistration = Normal(right.RegistrationNumber);
            if (leftRegistration.Length >= 6 && leftRegistration == rightRegistration) reasons.Add("Same registration/DHI ID");
            var leftNames = new[] { Normal(left.BarnName), Normal(left.RegisteredName) }.Where(value => value.Length > 1).ToHashSet();
            var rightNames = new[] { Normal(right.BarnName), Normal(right.RegisteredName) }.Where(value => value.Length > 1).ToHashSet();
            if (leftNames.Overlaps(rightNames)) reasons.Add("Same animal name");
            if (left.BirthDate.HasValue && left.BirthDate == right.BirthDate && (leftNames.Overlaps(rightNames) || Normal(left.DamName) == Normal(right.DamName))) reasons.Add("Same birth date and identity clues");
            var leftImportedIds = dataIds.Where(value => value.AnimalId == left.AnimalId).Select(value => Normal(value.OfficialId)).Where(value => value.Length >= 6).ToHashSet();
            var rightImportedIds = dataIds.Where(value => value.AnimalId == right.AnimalId).Select(value => Normal(value.OfficialId)).Where(value => value.Length >= 6).ToHashSet();
            if (leftImportedIds.Overlaps(rightImportedIds)) reasons.Add("Same imported official ID");
            if (reasons.Count > 0) animalFindings.Add(new { left = AnimalCard(left), right = AnimalCard(right), reasons = reasons.Distinct() });
        }

        var eventFindings = new List<object>();
        AddDuplicateEvents(eventFindings, "heat", await context.HeatEvents.AsNoTracking().Include(value => value.Animal).ToListAsync(ct), value => value.AnimalId, value => value.HeatDateTime.ToString("yyyy-MM-dd HH:mm"), value => value.HeatEventId, value => value.Animal?.DisplayName ?? $"#{value.AnimalId}");
        AddDuplicateEvents(eventFindings, "breeding", await context.BreedingEvents.AsNoTracking().Include(value => value.Animal).ToListAsync(ct), value => value.AnimalId, value => $"{value.BreedingDate:yyyy-MM-dd}|{Normal(value.SireUsed)}", value => value.BreedingEventId, value => value.Animal?.DisplayName ?? $"#{value.AnimalId}");
        AddDuplicateEvents(eventFindings, "calving", await context.CalvingEvents.AsNoTracking().Include(value => value.Animal).ToListAsync(ct), value => value.AnimalId, value => value.CalvingDate.ToString("yyyy-MM-dd"), value => value.CalvingEventId, value => value.Animal?.DisplayName ?? $"#{value.AnimalId}");
        AddDuplicateEvents(eventFindings, "dryOff", await context.DryOffEvents.AsNoTracking().Include(value => value.Animal).ToListAsync(ct), value => value.AnimalId, value => value.DryOffDate.ToString("yyyy-MM-dd"), value => value.DryOffEventId, value => value.Animal?.DisplayName ?? $"#{value.AnimalId}");
        AddDuplicateEvents(eventFindings, "lutalyse", await context.LutalyseEvents.AsNoTracking().Include(value => value.Animal).ToListAsync(ct), value => value.AnimalId, value => value.AdministrationDate.ToString("yyyy-MM-dd HH:mm"), value => value.LutalyseEventId, value => value.Animal?.DisplayName ?? $"#{value.AnimalId}");
        AddDuplicateEvents(eventFindings, "classification", await context.ClassificationRecords.AsNoTracking().Include(value => value.Animal).ToListAsync(ct), value => value.AnimalId, value => $"{value.ClassificationDate:yyyy-MM-dd}|{value.Score}", value => value.ClassificationRecordId, value => value.Animal?.DisplayName ?? $"#{value.AnimalId}");
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
    private static void AddDuplicateEvents<T>(List<object> output, string type, IEnumerable<T> values, Func<T, int> animalId, Func<T, string> signature, Func<T, int> id, Func<T, string> animalName)
    {
        foreach (var group in values.GroupBy(value => $"{animalId(value)}|{signature(value)}").Where(group => group.Count() > 1))
        { var records = group.ToList(); output.Add(new { eventType = type, animalId = animalId(records[0]), animalName = animalName(records[0]), signature = signature(records[0]), eventIds = records.Select(id).ToList(), count = records.Count }); }
    }
}

public sealed class MergeAnimalsRequest { public int KeepAnimalId { get; set; } public int RemoveAnimalId { get; set; } }
