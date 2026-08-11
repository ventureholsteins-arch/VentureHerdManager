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
    private static string Registration(string? value)
    {
        var normalized = Normal(value);
        return normalized.StartsWith("84000", StringComparison.Ordinal) ? normalized[5..] : normalized;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var denied = Guard(); if (denied != null) return denied;
        var animals = await context.Animals.AsNoTracking().Where(animal => animal.UpdatedBy != "Audit merge archive").ToListAsync(ct);
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
            var leftImportedIds = dataIds.Where(value => value.AnimalId == left.AnimalId).Select(value => Normal(value.OfficialId)).Where(value => value.Length >= 6).ToHashSet();
            var rightImportedIds = dataIds.Where(value => value.AnimalId == right.AnimalId).Select(value => Normal(value.OfficialId)).Where(value => value.Length >= 6).ToHashSet();
            if (leftImportedIds.Overlaps(rightImportedIds)) reasons.Add("Same imported official ID");
            var leftMappingIds = savedMappings.Where(value => value.AnimalId == left.AnimalId).Select(value => $"{value.Source}|{Normal(value.SourceKey)}").ToHashSet();
            var rightMappingIds = savedMappings.Where(value => value.AnimalId == right.AnimalId).Select(value => $"{value.Source}|{Normal(value.SourceKey)}").ToHashSet();
            if (leftMappingIds.Overlaps(rightMappingIds)) reasons.Add("Same saved PC-DART/Zoetis identity mapping");
            if (reasons.Count > 0) animalFindings.Add(new { left = AnimalCard(left), right = AnimalCard(right), reasons = reasons.Distinct() });
        }

        var eventFindings = new List<object>();
        AddNearEvents(eventFindings, "heat", await context.HeatEvents.AsNoTracking().Include(value => value.Animal).ToListAsync(ct), value => value.AnimalId, value => value.HeatDateTime, value => value.HeatEventId, value => value.Animal?.DisplayName ?? $"#{value.AnimalId}", value => $"strength {value.HeatStrength}|standing {value.StandingHeat}|{value.Notes}", value => new { value.HeatEventId, value.HeatDateTime, value.HeatStrength, value.StandingHeat, value.HasEmbryoTransfer, value.EmbryoImplantDate, value.Notes, value.CreatedAt }, 12);
        AddNearEvents(eventFindings, "breeding", await context.BreedingEvents.AsNoTracking().Include(value => value.Animal).ToListAsync(ct), value => value.AnimalId, value => value.BreedingDate, value => value.BreedingEventId, value => value.Animal?.DisplayName ?? $"#{value.AnimalId}", value => $"{Normal(value.SireUsed)}|{value.BreedingType}|{value.PregnancyStatus}|{value.Notes}", value => new { value.BreedingEventId, value.BreedingDate, value.SireUsed, value.BreedingType, value.PregnancyStatus, value.PregnancyCheckDate, value.ExpectedDueDate, value.RecommendedDryOffDate, value.Technician, value.Notes, value.CreatedAt }, 36);
        AddNearEvents(eventFindings, "calving", await context.CalvingEvents.AsNoTracking().Include(value => value.Animal).ToListAsync(ct), value => value.AnimalId, value => value.CalvingDate, value => value.CalvingEventId, value => value.Animal?.DisplayName ?? $"#{value.AnimalId}", value => $"{value.NumberOfCalves}|{value.CalfSex}|{value.CalfBarnName}|{value.Notes}", value => new { value.CalvingEventId, value.CalvingDate, value.CalfSex, value.CalfBarnName, value.CalfRegisteredName, value.CalfRegistrationNumber, value.CalvingEase, value.Twins, value.NumberOfCalves, value.Stillborn, value.BirthWeight, value.Notes, value.CreatedAt }, 48);
        AddNearEvents(eventFindings, "dryOff", await context.DryOffEvents.AsNoTracking().Include(value => value.Animal).ToListAsync(ct), value => value.AnimalId, value => value.DryOffDate, value => value.DryOffEventId, value => value.Animal?.DisplayName ?? $"#{value.AnimalId}", value => $"{value.Reason}|{value.Notes}", value => new { value.DryOffEventId, value.DryOffDate, value.Reason, value.Notes, value.CreatedAt }, 48);
        AddNearEvents(eventFindings, "lutalyse", await context.LutalyseEvents.AsNoTracking().Include(value => value.Animal).ToListAsync(ct), value => value.AnimalId, value => value.AdministrationDate, value => value.LutalyseEventId, value => value.Animal?.DisplayName ?? $"#{value.AnimalId}", value => $"{value.HeatObserved}|{value.Notes}", value => new { value.LutalyseEventId, value.AdministrationDate, value.ExpectedHeatWatchStart, value.ExpectedHeatWatchEnd, value.HeatObserved, value.HeatObservedDate, value.Notes, value.CreatedAt }, 24);
        AddNearEvents(eventFindings, "classification", await context.ClassificationRecords.AsNoTracking().Include(value => value.Animal).ToListAsync(ct), value => value.AnimalId, value => value.ClassificationDate ?? value.CreatedAt, value => value.ClassificationRecordId, value => value.Animal?.DisplayName ?? $"#{value.AnimalId}", value => $"{value.Score}|{value.Baa}|{value.ClassificationLabel}|{value.Notes}", value => new { value.ClassificationRecordId, value.ClassificationDate, value.Score, value.Baa, value.AgeInMonthsAtScoring, value.ClassificationLabel, value.Notes, value.CreatedAt }, 168);
        var missingSireFindings = animals
            .Where(animal => animal.AnimalStatus == AnimalStatus.Active
                && animal.Sex == AnimalSex.Female
                && animal.AnimalStage != AnimalStage.Bull
                && animal.SireId == null
                && string.IsNullOrWhiteSpace(animal.SireName))
            .OrderBy(animal => animal.DisplayName)
            .Select(animal => AnimalCard(animal))
            .ToList();
        var birthDateFindings = new List<object>();
        var birthFindingKeys = new HashSet<string>();
        var importedRecords = await context.AnimalDataRecords.AsNoTracking().Include(record => record.Animal).ToListAsync(ct);
        foreach (var record in importedRecords)
        {
            DateOnly? importedBirthDate = null;
            string? importedAge = null;
            try
            {
                using var document = System.Text.Json.JsonDocument.Parse(record.RawDataJson);
                foreach (var property in document.RootElement.EnumerateObject())
                {
                    if ((property.Name.Equals("BirthDate", StringComparison.OrdinalIgnoreCase) || property.Name.Equals("Birth Date", StringComparison.OrdinalIgnoreCase)) && DateOnly.TryParse(property.Value.ToString(), out var parsed)) importedBirthDate = parsed;
                    if (property.Name.Equals("AgeYRMO_Ref", StringComparison.OrdinalIgnoreCase)) importedAge = property.Value.ToString().Trim();
                }
            }
            catch (System.Text.Json.JsonException) { }
            if (importedBirthDate.HasValue && record.Animal.BirthDate != importedBirthDate && birthFindingKeys.Add($"date:{record.AnimalId}:{importedBirthDate}"))
                birthDateFindings.Add(new { record.AnimalId, AnimalName = record.Animal.DisplayName, CurrentBirthDate = record.Animal.BirthDate, ImportedBirthDate = importedBirthDate, ImportedAge = (string?)null, CurrentAgeAtReport = (string?)null, Source = record.Source.ToString(), record.ReportDate, record.SourceAnimalName });
            var ageParts = importedAge?.Split('-');
            if (record.Animal.BirthDate.HasValue && ageParts?.Length == 2 && int.TryParse(ageParts[0], out var years) && int.TryParse(ageParts[1], out var months))
            {
                var birth = record.Animal.BirthDate.Value; var currentMonths = (record.ReportDate.Year - birth.Year) * 12 + record.ReportDate.Month - birth.Month - (record.ReportDate.Day < birth.Day ? 1 : 0); var importedMonths = years * 12 + months;
                if (Math.Abs(currentMonths - importedMonths) > 1 && birthFindingKeys.Add($"age:{record.AnimalId}:{importedAge}"))
                    birthDateFindings.Add(new { record.AnimalId, AnimalName = record.Animal.DisplayName, CurrentBirthDate = record.Animal.BirthDate, ImportedBirthDate = (DateOnly?)null, ImportedAge = importedAge, CurrentAgeAtReport = $"{currentMonths / 12:00}-{currentMonths % 12:00}", Source = record.Source.ToString(), record.ReportDate, record.SourceAnimalName });
            }
        }
        var registrationFindings = importedRecords
            .Where(record => record.Source == HerdDataSource.Pcdart && Normal(record.OfficialId).Length >= 6 && Normal(record.OfficialId) != Normal(record.SourceAnimalName))
            .GroupBy(record => record.AnimalId)
            .Select(group => group.OrderByDescending(record => record.ReportDate).First())
            .Where(record => Registration(record.Animal.RegistrationNumber) != Registration(record.OfficialId))
            .OrderBy(record => record.Animal.DisplayName)
            .Select(record => new { record.AnimalId, AnimalName = record.Animal.DisplayName, CardRegistrationId = record.Animal.RegistrationNumber, PcdartRegistrationId = record.OfficialId, record.ReportDate, record.SourceAnimalName })
            .ToList();
        var latestCalvings = await context.CalvingEvents.AsNoTracking().GroupBy(value => value.AnimalId)
            .Select(group => new { AnimalId = group.Key, Date = group.Max(value => value.CalvingDate) }).ToDictionaryAsync(value => value.AnimalId, ct);
        var calvingDateFindings = importedRecords
            .Where(record => record.Source == HerdDataSource.Pcdart && record.LastCalvingDate.HasValue)
            .GroupBy(record => record.AnimalId)
            .Select(group => group.OrderByDescending(record => record.ReportDate).First())
            .Select(record => new { Record = record, AppCalving = latestCalvings.GetValueOrDefault(record.AnimalId) })
            .Where(value => value.AppCalving == null || DateOnly.FromDateTime(value.AppCalving.Date) != value.Record.LastCalvingDate)
            .OrderBy(value => value.Record.Animal.DisplayName)
            .Select(value => new { value.Record.AnimalId, AnimalName = value.Record.Animal.DisplayName, AppCalvingDate = value.AppCalving == null ? (DateOnly?)null : DateOnly.FromDateTime(value.AppCalving.Date), PcdartCalvingDate = value.Record.LastCalvingDate, value.Record.ReportDate, value.Record.SourceAnimalName })
            .ToList();
        return Ok(new { generatedAt = DateTime.UtcNow, mergeVersion = "atomic-archive-v2", animalFindings, eventFindings, missingSireFindings, birthDateFindings, registrationFindings, calvingDateFindings });
    }

    [HttpPost("merge")]
    public async Task<IActionResult> Merge(MergeAnimalsRequest request, CancellationToken ct)
    {
        var denied = Guard(); if (denied != null) return denied;
        try
        {
        if (request.KeepAnimalId == request.RemoveAnimalId) return BadRequest("Choose two different animal cards.");
        var keep = await context.Animals.SingleOrDefaultAsync(value => value.AnimalId == request.KeepAnimalId, ct);
        var remove = await context.Animals.SingleOrDefaultAsync(value => value.AnimalId == request.RemoveAnimalId, ct);
        if (keep == null || remove == null) return NotFound("One of the animal cards no longer exists.");
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
        var keptLifetimeByDate = await context.Set<LifetimeProductionSnapshot>().Where(value => value.AnimalId == keep.AnimalId).ToDictionaryAsync(value => value.ReportDate, ct);
        foreach (var value in await context.Set<LifetimeProductionSnapshot>().Where(value => value.AnimalId == remove.AnimalId).ToListAsync(ct))
        {
            if (keptLifetimeByDate.TryGetValue(value.ReportDate, out var existing))
            {
                existing.LifetimeMilk ??= value.LifetimeMilk; existing.LifetimeFat ??= value.LifetimeFat; existing.LifetimeProtein ??= value.LifetimeProtein; existing.Lactations ??= value.Lactations;
                context.Remove(value);
            }
            else { value.AnimalId = keep.AnimalId; keptLifetimeByDate[value.ReportDate] = value; }
        }
        remove.AnimalStatus = AnimalStatus.Sold;
        remove.UpdatedAt = DateTime.UtcNow;
        remove.UpdatedBy = "Audit merge archive";
        remove.Notes = string.Join("\n", new[] { remove.Notes, $"Duplicate card archived after its records were merged into animal #{keep.AnimalId} ({keep.DisplayName}) on {DateTime.UtcNow:yyyy-MM-dd}." }.Where(value => !string.IsNullOrWhiteSpace(value)));
        await context.SaveChangesAsync(ct);
        return Ok(new { keptAnimalId = keep.AnimalId, removedAnimalId = remove.AnimalId });
        }
        catch (Exception exception)
        {
            return BadRequest($"The cards were not merged and no data was changed: {exception.GetBaseException().Message}");
        }
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

    [HttpPost("accept-pcdart")]
    public async Task<IActionResult> AcceptPcdart(AcceptPcdartAuditRequest request, CancellationToken ct)
    {
        var denied = Guard(); if (denied != null) return denied;
        var animal = await context.Animals.SingleOrDefaultAsync(value => value.AnimalId == request.AnimalId, ct);
        if (animal == null) return NotFound("The animal card no longer exists.");
        var latest = await context.AnimalDataRecords.AsNoTracking().Where(value => value.AnimalId == request.AnimalId && value.Source == HerdDataSource.Pcdart).OrderByDescending(value => value.ReportDate).FirstOrDefaultAsync(ct);
        if (latest == null) return BadRequest("No PC-DART record is stored for this animal.");
        switch (request.Field.Trim().ToLowerInvariant())
        {
            case "registration":
                if (string.IsNullOrWhiteSpace(latest.OfficialId)) return BadRequest("PC-DART did not supply a usable registration/DHI ID.");
                animal.RegistrationNumber = latest.OfficialId.Trim();
                break;
            case "birthdate":
                DateOnly? importedBirth = null;
                try { using var document = System.Text.Json.JsonDocument.Parse(latest.RawDataJson); foreach (var property in document.RootElement.EnumerateObject()) if ((property.Name.Equals("BirthDate", StringComparison.OrdinalIgnoreCase) || property.Name.Equals("Birth Date", StringComparison.OrdinalIgnoreCase)) && DateOnly.TryParse(property.Value.ToString(), out var parsed)) importedBirth = parsed; } catch (System.Text.Json.JsonException) { }
                if (!importedBirth.HasValue) return BadRequest("PC-DART did not supply an exact birthdate.");
                animal.BirthDate = importedBirth;
                break;
            case "calvingdate":
                if (!latest.LastCalvingDate.HasValue) return BadRequest("PC-DART did not supply a calving date.");
                var calving = await context.CalvingEvents.Where(value => value.AnimalId == animal.AnimalId).OrderByDescending(value => value.CalvingDate).FirstOrDefaultAsync(ct);
                if (calving == null) context.CalvingEvents.Add(new CalvingEvent { AnimalId = animal.AnimalId, CalvingDate = latest.LastCalvingDate.Value.ToDateTime(new TimeOnly(12, 0)), Notes = "Calving date accepted from PC-DART audit.", CreatedBy = "Accepted PC-DART audit" });
                else { calving.CalvingDate = latest.LastCalvingDate.Value.ToDateTime(TimeOnly.FromDateTime(calving.CalvingDate)); calving.UpdatedAt = DateTime.UtcNow; calving.UpdatedBy = "Accepted PC-DART audit"; }
                animal.AnimalStage = AnimalStage.Milking;
                break;
            default: return BadRequest("Choose birthdate, registration, or calving date.");
        }
        animal.UpdatedAt = DateTime.UtcNow; animal.UpdatedBy = "Accepted PC-DART audit";
        await context.SaveChangesAsync(ct);
        return Ok(new { request.AnimalId, request.Field, accepted = true });
    }

    private static object AnimalCard(Animal animal) => new { animal.AnimalId, animal.BarnName, animal.RegisteredName, animal.RegistrationNumber, animal.BirthDate, animal.DamName, animal.SireName, animal.CreatedAt };
    private static void AddNearEvents<T>(List<object> output, string type, IEnumerable<T> values, Func<T, int> animalId, Func<T, DateTime> date, Func<T, int> id, Func<T, string> animalName, Func<T, string> comparisonDetail, Func<T, object> snapshot, double maximumHours)
    {
        foreach (var group in values.GroupBy(animalId))
        {
            var records = group.OrderBy(date).ToList();
            for (var left = 0; left < records.Count; left++) for (var right = left + 1; right < records.Count; right++)
            {
                var hours = Math.Abs((date(records[right]) - date(records[left])).TotalHours); if (hours > maximumHours) break;
                var timing = hours < .02 ? "same timestamp" : hours < 24 ? $"{hours:0.#} hours apart" : $"{hours / 24:0.#} days apart";
                var sameDetails = Normal(comparisonDetail(records[left])) == Normal(comparisonDetail(records[right]));
                var confidence = hours < .02 && sameDetails ? "Likely duplicate" : sameDetails ? "Strong similarity" : "Review only";
                output.Add(new { eventType = type, animalId = group.Key, animalName = animalName(records[left]), signature = $"{date(records[left]):MMM d, yyyy h:mm tt} and {date(records[right]):MMM d, yyyy h:mm tt} - {timing}", eventIds = new[] { id(records[left]), id(records[right]) }, records = new[] { snapshot(records[left]), snapshot(records[right]) }, count = 2, confidence, reviewReason = sameDetails ? $"Two {type} records have matching details and close dates" : $"Two {type} records have close dates but different details" });
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
public sealed class AcceptPcdartAuditRequest { public int AnimalId { get; set; } public string Field { get; set; } = string.Empty; }
