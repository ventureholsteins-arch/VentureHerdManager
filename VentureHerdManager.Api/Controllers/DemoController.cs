using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VentureHerdManager.Api.Data;
using VentureHerdManager.Api.Models;

namespace VentureHerdManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableCors("Frontend")]
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
        var enabled = _configuration.GetValue<bool>("DemoMode:Enabled");

        var animalCount = await _context.Animals.CountAsync(cancellationToken);
        var heatCount = await _context.HeatEvents.CountAsync(cancellationToken);
        var breedingCount = await _context.BreedingEvents.CountAsync(cancellationToken);
        var calvingCount = await _context.CalvingEvents.CountAsync(cancellationToken);

        return Ok(new
        {
            enabled,
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
    public async Task<ActionResult<DemoSeedResult>> Reset(
        [FromHeader(Name = "X-Demo-Key")] string? demoKey,
        CancellationToken cancellationToken)
    {
        var guardResult = ValidateDemoAccess(demoKey);
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
        var seedUser = "DemoSeeder";

        var demoAnimals = new List<Animal>
        {
            new()
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
                CreatedBy = seedUser,
                UpdatedBy = seedUser,
                IsFavorite = true
            },
            new()
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
            },
            new()
            {
                BarnName = "Venture Ember",
                RegisteredName = "Venture Ember 612",
                RegistrationNumber = "DEMO-612",
                Sex = AnimalSex.Female,
                AnimalStage = AnimalStage.Dry,
                AnimalStatus = AnimalStatus.Active,
                Breed = "Jersey",
                BirthDate = DateOnly.FromDateTime(utcNow.AddYears(-4).AddMonths(-6)),
                CurrentLactation = 2,
                CreatedBy = seedUser,
                UpdatedBy = seedUser
            },
            new()
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
            },
            new()
            {
                BarnName = "Venture Clover",
                RegisteredName = "Venture Clover 198",
                RegistrationNumber = "DEMO-198",
                Sex = AnimalSex.Female,
                AnimalStage = AnimalStage.Calf,
                AnimalStatus = AnimalStatus.Active,
                Breed = "Holstein",
                BirthDate = DateOnly.FromDateTime(utcNow.AddMonths(-4)),
                CreatedBy = seedUser,
                UpdatedBy = seedUser
            },
            new()
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
            }
        };

        _context.Animals.AddRange(demoAnimals);
        await _context.SaveChangesAsync(cancellationToken);

        var aurora = demoAnimals.First(a => a.BarnName == "Venture Aurora");
        var nova = demoAnimals.First(a => a.BarnName == "Venture Nova");
        var ember = demoAnimals.First(a => a.BarnName == "Venture Ember");
        var daisy = demoAnimals.First(a => a.BarnName == "Venture Daisy");

        var demoCalf = new Animal
        {
            BarnName = "Venture Spark",
            RegisteredName = "Venture Spark 001",
            RegistrationNumber = "DEMO-CALF-001",
            Sex = AnimalSex.Female,
            AnimalStage = AnimalStage.Calf,
            AnimalStatus = AnimalStatus.Active,
            Breed = "Holstein",
            BirthDate = DateOnly.FromDateTime(utcNow.AddDays(-2)),
            SireName = "Venture Titan 900",
            DamId = aurora.AnimalId,
            DamName = aurora.RegisteredName ?? aurora.BarnName,
            CreatedBy = seedUser,
            UpdatedBy = seedUser,
            ProfilePictureUrl = "https://picsum.photos/seed/venture-calf/900/600"
        };

        _context.Animals.Add(demoCalf);
        await _context.SaveChangesAsync(cancellationToken);

        var heats = new List<HeatEvent>
        {
            new()
            {
                AnimalId = aurora.AnimalId,
                HeatDateTime = utcNow.AddDays(-7),
                HeatStrength = HeatStrength.Strong,
                StandingHeat = true,
                HasEmbryoTransfer = true,
                EmbryoImplantDate = utcNow,
                Notes = "Demo heat event for walkthrough.",
                CreatedBy = seedUser,
                UpdatedBy = seedUser
            },
            new()
            {
                AnimalId = daisy.AnimalId,
                HeatDateTime = utcNow.AddDays(-2),
                HeatStrength = HeatStrength.Normal,
                StandingHeat = true,
                Notes = "Observed during morning checks.",
                CreatedBy = seedUser,
                UpdatedBy = seedUser
            }
        };

        _context.HeatEvents.AddRange(heats);

        var breedings = new List<BreedingEvent>
        {
            new()
            {
                AnimalId = nova.AnimalId,
                BreedingDate = utcNow.AddDays(-40),
                SireUsed = "Demonstrator-ET-11",
                BreedingType = BreedingType.AI,
                PregnancyStatus = PregnancyStatus.Pregnant,
                PregnancyCheckDueDate = utcNow.AddDays(-10),
                ExpectedDueDate = utcNow.AddDays(22),
                RecommendedDryOffDate = utcNow.AddDays(-38),
                CloseUpDate = utcNow.AddDays(1),
                Notes = "Demo pregnant cow due soon.",
                CreatedBy = seedUser,
                UpdatedBy = seedUser
            },
            new()
            {
                AnimalId = ember.AnimalId,
                BreedingDate = utcNow.AddDays(-26),
                SireUsed = "Venture Titan 900",
                BreedingType = BreedingType.Natural,
                PregnancyStatus = PregnancyStatus.Recheck,
                PregnancyCheckDueDate = utcNow.AddDays(3),
                ExpectedDueDate = utcNow.AddDays(257),
                Notes = "Recheck needed in demo dashboard.",
                CreatedBy = seedUser,
                UpdatedBy = seedUser
            }
        };

        _context.BreedingEvents.AddRange(breedings);

        _context.LutalyseEvents.Add(new LutalyseEvent
        {
            AnimalId = daisy.AnimalId,
            AdministrationDate = utcNow.AddDays(-1),
            ExpectedHeatWatchStart = utcNow,
            ExpectedHeatWatchEnd = utcNow.AddDays(3),
            HeatObserved = false,
            Notes = "Demo LUT tracking item.",
            CreatedBy = seedUser,
            UpdatedBy = seedUser
        });

        var calvingEvent = new CalvingEvent
        {
            AnimalId = aurora.AnimalId,
            CalvingDate = utcNow.AddDays(-2),
            CalfSex = CalfSex.Heifer,
            CalfBarnName = demoCalf.BarnName,
            CalfRegisteredName = demoCalf.RegisteredName,
            CalfAnimalId = demoCalf.AnimalId,
            CalvingEase = CalvingEase.Unassisted,
            NumberOfCalves = 1,
            Twins = false,
            Stillborn = false,
            BirthWeight = 86.5m,
            PictureUrl = "https://picsum.photos/seed/venture-calf/900/600",
            Notes = "Healthy demo calf. Good appetite.",
            CreatedBy = seedUser,
            UpdatedBy = seedUser
        };

        _context.CalvingEvents.Add(calvingEvent);

        _context.AnimalNotes.AddRange(
            new AnimalNote
            {
                AnimalId = aurora.AnimalId,
                NoteDate = utcNow.AddDays(-1),
                NoteText = "Demo note: appetite and milk output normal.",
                NoteType = NoteType.General,
                CreatedBy = seedUser
            },
            new AnimalNote
            {
                AnimalId = nova.AnimalId,
                NoteDate = utcNow.AddDays(-2),
                NoteText = "Demo note: close-up ration adjusted.",
                NoteType = NoteType.Health,
                CreatedBy = seedUser
            });

        _context.ClassificationRecords.AddRange(
            new ClassificationRecord
            {
                AnimalId = aurora.AnimalId,
                ClassificationDate = utcNow.AddMonths(-2),
                Score = 91.0m,
                Baa = 109.4m,
                ClassificationLabel = "EX",
                Notes = "Demo elite score",
                CreatedBy = seedUser,
                UpdatedBy = seedUser,
                UpdatedAt = utcNow
            },
            new ClassificationRecord
            {
                AnimalId = nova.AnimalId,
                ClassificationDate = utcNow.AddMonths(-3),
                Score = 88.0m,
                Baa = 104.2m,
                ClassificationLabel = "VG",
                Notes = "Demo classification",
                CreatedBy = seedUser,
                UpdatedBy = seedUser,
                UpdatedAt = utcNow
            });

        await _context.SaveChangesAsync(cancellationToken);

        _context.AnimalPhotos.AddRange(
            new AnimalPhoto
            {
                AnimalId = aurora.AnimalId,
                PhotoUrl = calvingEvent.PictureUrl!,
                PhotoType = AnimalPhotoType.Calving,
                RelatedEventId = calvingEvent.CalvingEventId,
                RelatedEventType = nameof(CalvingEvent),
                Caption = "Demo calving photo",
                CreatedBy = seedUser
            },
            new AnimalPhoto
            {
                AnimalId = demoCalf.AnimalId,
                PhotoUrl = calvingEvent.PictureUrl!,
                PhotoType = AnimalPhotoType.Calf,
                RelatedEventId = calvingEvent.CalvingEventId,
                RelatedEventType = nameof(CalvingEvent),
                Caption = "Demo calf profile photo",
                CreatedBy = seedUser
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

    private ActionResult<DemoSeedResult>? ValidateDemoAccess(string? providedKey)
    {
        var enabled = _configuration.GetValue<bool>("DemoMode:Enabled");
        if (!enabled)
        {
            return BadRequest(new DemoSeedResult
            {
                Message = "DemoMode is disabled."
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
