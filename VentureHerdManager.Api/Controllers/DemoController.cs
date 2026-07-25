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

        var activeCount = await _context.Animals
            .CountAsync(a => a.AnimalStatus == AnimalStatus.Active, cancellationToken);

        var stageCounts = await _context.Animals
            .GroupBy(a => a.AnimalStage)
            .Select(group => new
            {
                stage = group.Key.ToString(),
                count = group.Count()
            })
            .ToListAsync(cancellationToken);

        var previewAnimals = await _context.Animals
            .OrderBy(a => a.BarnName)
            .Take(8)
            .Select(a => new
            {
                a.AnimalId,
                name = a.BarnName ?? a.RegisteredName ?? $"Animal {a.AnimalId}",
                stage = a.AnimalStage.ToString(),
                a.Breed
            })
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            enabled = true,
            counts = new
            {
                animals = animalCount,
                activeAnimals = activeCount,
                heats = heatCount,
                breedings = breedingCount,
                calvings = calvingCount,
                lutalyseEvents = await _context.LutalyseEvents.CountAsync(cancellationToken),
                notes = await _context.AnimalNotes.CountAsync(cancellationToken),
                photos = await _context.AnimalPhotos.CountAsync(cancellationToken)
            },
            stageCounts,
            previewAnimals
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
        var random = new Random(20260724);

        static string MakeRegistration(string prefix, int number) => $"{prefix}-{number:000}";

        int NextInt(int minInclusive, int maxExclusive) => random.Next(minInclusive, maxExclusive);

        var demoCows = new List<Animal>
        {
            new()
            {
                BarnName = "Venture Aurora",
                RegisteredName = "Venture Aurora 501",
                RegistrationNumber = MakeRegistration("DEMO", 501),
                Sex = AnimalSex.Female,
                AnimalStage = AnimalStage.Milking,
                AnimalStatus = AnimalStatus.Active,
                Breed = "Holstein",
                BirthDate = DateOnly.FromDateTime(utcNow.AddYears(-4)),
                CurrentLactation = NextInt(2, 5),
                IsFavorite = true,
                Notes = "Top producing cow",
                CreatedBy = seedUser,
                UpdatedBy = seedUser
            },
            new()
            {
                BarnName = "Venture Nova",
                RegisteredName = "Venture Nova 327",
                RegistrationNumber = MakeRegistration("DEMO", 327),
                Sex = AnimalSex.Female,
                AnimalStage = AnimalStage.Milking,
                AnimalStatus = AnimalStatus.Active,
                Breed = "Jersey",
                BirthDate = DateOnly.FromDateTime(utcNow.AddYears(-5)),
                CurrentLactation = NextInt(1, 5),
                CreatedBy = seedUser,
                UpdatedBy = seedUser
            },
            new()
            {
                BarnName = "Venture Daisy",
                RegisteredName = "Venture Daisy 214",
                RegistrationNumber = MakeRegistration("DEMO", 214),
                Sex = AnimalSex.Female,
                AnimalStage = AnimalStage.Dry,
                AnimalStatus = AnimalStatus.Active,
                Breed = "Holstein",
                BirthDate = DateOnly.FromDateTime(utcNow.AddYears(-3).AddMonths(-4)),
                CurrentLactation = NextInt(2, 5),
                CreatedBy = seedUser,
                UpdatedBy = seedUser
            },
            new()
            {
                BarnName = "Venture Clover",
                RegisteredName = "Venture Clover 198",
                RegistrationNumber = MakeRegistration("DEMO", 198),
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
                BarnName = "Venture Ember",
                RegisteredName = "Venture Ember 612",
                RegistrationNumber = MakeRegistration("DEMO", 612),
                Sex = AnimalSex.Female,
                AnimalStage = AnimalStage.Milking,
                AnimalStatus = AnimalStatus.Active,
                Breed = "Ayrshire",
                BirthDate = DateOnly.FromDateTime(utcNow.AddYears(-4).AddMonths(-8)),
                CurrentLactation = NextInt(1, 4),
                CreatedBy = seedUser,
                UpdatedBy = seedUser
            }
        };

        _context.Animals.AddRange(demoCows);
        await _context.SaveChangesAsync(cancellationToken);

        var aurora = demoCows[0];
        var nova = demoCows[1];
        var daisy = demoCows[2];
        var clover = demoCows[3];
        var ember = demoCows[4];

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
            SireName = ember.RegisteredName ?? ember.BarnName,
            DamId = aurora.AnimalId,
            DamName = aurora.RegisteredName ?? aurora.BarnName,
            ProfilePictureUrl = "/Seashell_cow.jpg",
            CreatedBy = seedUser,
            UpdatedBy = seedUser
        };

        _context.Animals.Add(demoCalf);
        await _context.SaveChangesAsync(cancellationToken);

        _context.HeatEvents.AddRange(
            new HeatEvent
            {
                AnimalId = aurora.AnimalId,
                HeatDateTime = utcNow.AddDays(-3),
                HeatStrength = HeatStrength.Strong,
                StandingHeat = true,
                HasEmbryoTransfer = true,
                EmbryoImplantDate = utcNow.AddDays(1),
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
            SireUsed = ember.RegisteredName ?? ember.BarnName ?? "Demo Sire",
            BreedingType = BreedingType.AI,
            PregnancyStatus = PregnancyStatus.Pregnant,
            PregnancyCheckDueDate = utcNow.AddDays(-2),
            ExpectedDueDate = utcNow.AddDays(250),
            RecommendedDryOffDate = utcNow.AddDays(220),
            CloseUpDate = utcNow.AddDays(235),
            Notes = "Demo breeding event",
            CreatedBy = seedUser,
            UpdatedBy = seedUser
        });

        _context.DryOffEvents.Add(new DryOffEvent
        {
            AnimalId = ember.AnimalId,
            DryOffDate = utcNow.AddDays(-8),
            Reason = "Upcoming calving prep",
            Notes = "Demo dry-off event",
            CreatedBy = seedUser,
            UpdatedBy = seedUser
        });

        _context.LutalyseEvents.Add(new LutalyseEvent
        {
            AnimalId = clover.AnimalId,
            AdministrationDate = utcNow.AddDays(-5),
            ExpectedHeatWatchStart = utcNow.AddDays(-4),
            ExpectedHeatWatchEnd = utcNow.AddDays(-2),
            HeatObserved = true,
            HeatObservedDate = utcNow.AddDays(-3),
            Notes = "Demo LUT tracking event",
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
            BirthWeight = Math.Round((decimal)(70 + random.NextDouble() * 25), 1),
            PictureUrl = "/Seashell_cow.jpg",
            Notes = "Healthy demo calf",
            CreatedBy = seedUser,
            UpdatedBy = seedUser
        };

        _context.CalvingEvents.Add(calvingEvent);

        _context.AnimalNotes.Add(new AnimalNote
        {
            AnimalId = aurora.AnimalId,
            NoteDate = utcNow,
            NoteText = "Demo note: healthy and producing well.",
            NoteType = NoteType.General,
            CreatedBy = seedUser
        });

        _context.ClassificationRecords.AddRange(
            new ClassificationRecord
            {
                AnimalId = aurora.AnimalId,
                ClassificationDate = utcNow.AddMonths(-2),
                Score = 91m,
                Baa = 109.4m,
                AgeInMonthsAtScoring = 52,
                ClassificationLabel = "EX",
                Notes = "Demo classification",
                CreatedBy = seedUser,
                UpdatedBy = seedUser,
                UpdatedAt = utcNow
            },
            new ClassificationRecord
            {
                AnimalId = nova.AnimalId,
                ClassificationDate = utcNow.AddMonths(-3),
                Score = 88m,
                Baa = 104.2m,
                AgeInMonthsAtScoring = 60,
                ClassificationLabel = "VG",
                Notes = "Demo classification",
                CreatedBy = seedUser,
                UpdatedBy = seedUser,
                UpdatedAt = utcNow
            });

        _context.AnimalPhotos.AddRange(
            new AnimalPhoto
            {
                AnimalId = aurora.AnimalId,
                PhotoUrl = "/Seashell_cow.jpg",
                PhotoType = AnimalPhotoType.Profile,
                Caption = "Demo cow profile photo",
                CreatedBy = seedUser
            },
            new AnimalPhoto
            {
                AnimalId = demoCalf.AnimalId,
                PhotoUrl = "/Seashell_cow.jpg",
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
