using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VentureHerdManager.Api.Data;
using VentureHerdManager.Api.Services;

namespace VentureHerdManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ShareLinksController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly DemoSessionContext _demoSession;
    private readonly IDataProtector _protector;

    public ShareLinksController(
        ApplicationDbContext context,
        DemoSessionContext demoSession,
        IDataProtectionProvider protectionProvider)
    {
        _context = context;
        _demoSession = demoSession;
        _protector = protectionProvider.CreateProtector(
            "VentureHerdManager.ReadOnlyShare.v1");
    }

    [HttpPost]
    public ActionResult<CreateShareLinkResponse> Create(CreateShareLinkRequest request)
    {
        var animalIds = request.AnimalIds
            .Where(id => id > 0)
            .Distinct()
            .Take(500)
            .ToArray();
        if (animalIds.Length == 0)
        {
            return BadRequest("Select at least one animal to share.");
        }

        var payload = new SharePayload(
            animalIds,
            request.IncludeAnimals,
            request.IncludeEmbryos,
            request.IncludeOutcomes,
            DateTime.UtcNow.AddDays(Math.Clamp(request.ExpiresInDays, 1, 30)),
            _demoSession.IsDemoMode ? _demoSession.SessionId : null);
        var token = _protector.Protect(JsonSerializer.Serialize(payload));
        return Ok(new CreateShareLinkResponse(token, payload.ExpiresAt));
    }

    [HttpGet("{token}")]
    public async Task<IActionResult> Read(string token)
    {
        SharePayload payload;
        try
        {
            payload = JsonSerializer.Deserialize<SharePayload>(
                _protector.Unprotect(token))
                ?? throw new InvalidOperationException();
        }
        catch
        {
            return NotFound("This share link is invalid or has expired.");
        }

        if (payload.ExpiresAt < DateTime.UtcNow)
        {
            return StatusCode(StatusCodes.Status410Gone, "This share link has expired.");
        }

        var ids = payload.AnimalIds.ToHashSet();
        var animalQuery = _context.Animals.AsNoTracking()
            .Where(animal => ids.Contains(animal.AnimalId));
        var embryoQuery = _context.EmbryoRecords.AsNoTracking()
            .Where(embryo =>
                (embryo.RecipientAnimalId.HasValue
                 && ids.Contains(embryo.RecipientAnimalId.Value))
                || (embryo.DonorAnimalId.HasValue
                    && ids.Contains(embryo.DonorAnimalId.Value)));

        if (!string.IsNullOrWhiteSpace(payload.DemoSessionId))
        {
            animalQuery = animalQuery.Where(animal =>
                EF.Property<string?>(animal, "DemoSessionId")
                    == payload.DemoSessionId);
            embryoQuery = embryoQuery.Where(embryo =>
                EF.Property<string?>(embryo, "DemoSessionId")
                    == payload.DemoSessionId);
        }

        var animals = payload.IncludeAnimals
            ? await animalQuery
                .OrderBy(animal => animal.BarnName)
                .Select(animal => new
                {
                    animal.AnimalId,
                    Name = animal.BarnName
                        ?? animal.RegisteredName
                        ?? ((animal.SireName ?? "Unknown sire")
                            + " x " + (animal.DamName ?? "Unknown dam")),
                    animal.RegisteredName,
                    animal.RegistrationNumber,
                    animal.BirthDate,
                    animal.AnimalStage,
                    animal.Breed,
                    animal.SireName,
                    animal.DamName
                })
                .ToListAsync()
            : [];

        var embryos = payload.IncludeEmbryos
            ? await embryoQuery
                .OrderByDescending(embryo => embryo.ImplantDate)
                .Select(embryo => new
                {
                    embryo.EmbryoRecordId,
                    embryo.Code,
                    embryo.Donor,
                    embryo.Sire,
                    embryo.Grade,
                    embryo.Status,
                    embryo.RecipientAnimalId,
                    embryo.ImplantDate,
                    Outcome = payload.IncludeOutcomes
                        ? embryo.Status.ToString()
                        : null
                })
                .ToListAsync()
            : [];

        return Ok(new
        {
            ReadOnly = true,
            payload.ExpiresAt,
            Animals = animals,
            Embryos = embryos
        });
    }

    private sealed record SharePayload(
        int[] AnimalIds,
        bool IncludeAnimals,
        bool IncludeEmbryos,
        bool IncludeOutcomes,
        DateTime ExpiresAt,
        string? DemoSessionId);
}

public sealed class CreateShareLinkRequest
{
    public int[] AnimalIds { get; set; } = [];
    public bool IncludeAnimals { get; set; } = true;
    public bool IncludeEmbryos { get; set; } = true;
    public bool IncludeOutcomes { get; set; } = true;
    public int ExpiresInDays { get; set; } = 14;
}

public sealed record CreateShareLinkResponse(string Token, DateTime ExpiresAt);
