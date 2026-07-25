using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VentureHerdManager.Api.Data;
using VentureHerdManager.Api.Models;

namespace VentureHerdManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DemoController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public DemoController(
        ApplicationDbContext context,
        IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    [HttpGet("status")]
    public async Task<ActionResult<object>> Status(CancellationToken cancellationToken)
    {
        var guardResult = ValidateDemoAccess();
        if (guardResult != null)
        {
            return guardResult;
        }

        var animalCount = await _context.Animals.CountAsync(cancellationToken);
        var heatCount = await _context.HeatEvents.CountAsync(cancellationToken);
        var breedingCount = await _context.BreedingEvents.CountAsync(cancellationToken);
        var calvingCount = await _context.CalvingEvents.CountAsync(cancellationToken);

        return Ok(new
        {
            enabled = true,
            counts = new
            {
                animals = animalCount,
                heats = heatCount,
                breedings = breedingCount,
                calvings = calvingCount
            }
        });
    }

    [HttpPost("reset")]
    public async Task<ActionResult<DemoSeedResult>> Reset(CancellationToken cancellationToken)
    {
        var guardResult = ValidateDemoAccess();
        if (guardResult != null)
        {
            return guardResult;
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync(cancellationToken);

        await _context.AnimalPhotos.ExecuteDeleteAsync(cancellationToken);
        await _context.AnimalNotes.ExecuteDeleteAsync(cancellationToken);
        await _context.ClassificationRecords.ExecuteDeleteAsync(cancellationToken);
        await _context.HeatEvents.ExecuteDeleteAsync(cancellationToken);
        await _context.BreedingEvents.ExecuteDeleteAsync(cancellationToken);
        await _context.DryOffEvents.ExecuteDeleteAsync(cancellationToken);
        await _context.LutalyseEvents.ExecuteDeleteAsync(cancellationToken);
        await _context.CalvingEvents.ExecuteDeleteAsync(cancellationToken);

        await _context.Animals.ExecuteUpdateAsync(
            setters => setters
                .SetProperty(a => a.DamId, (int?)null)
                .SetProperty(a => a.SireId, (int?)null),
            cancellationToken);

        await _context.Animals.ExecuteDeleteAsync(cancellationToken);

        var utcNow = DateTime.UtcNow;
        const string seedUser = "DemoSeeder";

        var aurora = new Animal
        {
            BarnName = "Venture Aurora",
            RegisteredName = "Venture Aurora 501",
            RegistrationNumber = "DEMO-501",
            Sex = AnimalSex.Female,
            AnimalStage = AnimalStage.Milking,
            AnimalStatus = AnimalStatus.Active,
            Breed = "Holstein",
            BirthDate = DateOnly.FromDateTime(utcNow.AddYears(-4)),
            CurrentLactation = 2,
            IsFavorite = true,
            CreatedBy = seedUser,
            UpdatedBy = seedUser
        };

        var nova = new Animal
        {
            BarnName = "Venture Nova",
            RegisteredName = "Venture Nova 327",
            RegistrationNumber = "DEMO-327",
            Sex = AnimalSex.Female,
            AnimalStage = AnimalStage.Milking,
            AnimalStatus = AnimalStatus.Active,
            Breed = "Holstein",
            BirthDate = DateOnly.FromDateTime(utcNow.AddYears(-5)),
            CurrentLactation = 3,
            CreatedBy = seedUser,
            UpdatedBy = seedUser
        };

        var daisy = new Animal
        {
            BarnName = "Venture Daisy",
            RegisteredName = "Venture Daisy 214",
            RegistrationNumber = "DEMO-214",
            Sex = AnimalSex.Female,
            AnimalStage = AnimalStage.Heifer,
            AnimalStatus = AnimalStatus.Active,
            Breed = "Holstein",
            BirthDate = DateOnly.FromDateTime(utcNow.AddYears(-2)),
            CreatedBy = seedUser,
            UpdatedBy = seedUser
        };

        var titan = new Animal
        {
            BarnName = "Venture Titan",
            RegisteredName = "Venture Titan 900",
            RegistrationNumber = "DEMO-900",
            Sex = AnimalSex.Male,
            AnimalStage = AnimalStage.Bull,
            AnimalStatus = AnimalStatus.Active,
            Breed = "Holstein",
            BirthDate = DateOnly.FromDateTime(utcNow.AddYears(-3)),
            CreatedBy = seedUser,
            UpdatedBy = seedUser
        };

        _context.Animals.AddRange(aurora, nova, daisy, titan);
        await _context.SaveChangesAsync(cancellationToken);

        _context.HeatEvents.AddRange(
            new HeatEvent
            {
                AnimalId = aurora.AnimalId,
                HeatDateTime = utcNow.AddDays(-3),
                HeatStrength = HeatStrength.Strong,
                StandingHeat = true,
                Notes = "Demo heat event",
                CreatedBy = seedUser,
                UpdatedBy = seedUser
            },
            new HeatEvent
            {
                AnimalId = daisy.AnimalId,
                HeatDateTime = utcNow.AddDays(-1),
                HeatStrength = HeatStrength.Normal,
                StandingHeat = true,
                Notes = "Demo heifer heat",
                CreatedBy = seedUser,
                UpdatedBy = seedUser
            });

        _context.BreedingEvents.Add(new BreedingEvent
        {
            AnimalId = nova.AnimalId,
            BreedingDate = utcNow.AddDays(-30),
            SireUsed = titan.RegisteredName ?? titan.BarnName ?? "Demo Sire",
            BreedingType = BreedingType.AI,
            PregnancyStatus = PregnancyStatus.Pregnant,
            ExpectedDueDate = utcNow.AddDays(250),
            Notes = "Demo breeding event",
            CreatedBy = seedUser,
            UpdatedBy = seedUser
        });

        _context.AnimalNotes.Add(new AnimalNote
        {
            AnimalId = aurora.AnimalId,
            NoteDate = utcNow,
            NoteText = "Demo note: healthy and producing well.",
            NoteType = NoteType.General,
            CreatedBy = seedUser
        });

        _context.ClassificationRecords.Add(new ClassificationRecord
        {
            AnimalId = aurora.AnimalId,
            ClassificationDate = utcNow.AddMonths(-2),
            Score = 91m,
            Baa = 109.4m,
            ClassificationLabel = "EX",
            Notes = "Demo classification",
            CreatedBy = seedUser,
            UpdatedBy = seedUser,
            UpdatedAt = utcNow
        });

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Ok(new DemoSeedResult
        {
            Message = "Demo data reset and seeded.",
            Animals = await _context.Animals.CountAsync(cancellationToken),
            HeatEvents = await _context.HeatEvents.CountAsync(cancellationToken),
            BreedingEvents = await _context.BreedingEvents.CountAsync(cancellationToken),
            CalvingEvents = await _context.CalvingEvents.CountAsync(cancellationToken),
            LutalyseEvents = await _context.LutalyseEvents.CountAsync(cancellationToken)
        });
    }

    private ActionResult? ValidateDemoAccess()
    {
        var enabled = _configuration.GetValue<bool>("DemoMode:Enabled");
        if (!enabled)
        {
            return StatusCode(403, new DemoSeedResult
            {
                Message = "DemoMode is disabled."
            });
        }

        var demoConnectionString =
            _configuration["ConnectionStrings__DemoConnection"]
            ?? _configuration.GetConnectionString("DemoConnection")
            ?? _configuration["ConnectionStrings:DemoConnection"];

        if (string.IsNullOrWhiteSpace(demoConnectionString))
        {
            return StatusCode(403, new DemoSeedResult
            {
                Message = "DemoConnection is not configured."
            });
        }

        return null;
    }
}

public class DemoSeedResult
{
    public string Message { get; set; } = string.Empty;

    public int Animals { get; set; }

    public int HeatEvents { get; set; }

    public int BreedingEvents { get; set; }

    public int CalvingEvents { get; set; }

    public int LutalyseEvents { get; set; }
}
