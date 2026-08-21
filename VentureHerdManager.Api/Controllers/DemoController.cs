using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VentureHerdManager.Api.Data;
using VentureHerdManager.Api.Models;
using VentureHerdManager.Api.Services;

namespace VentureHerdManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DemoController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly DemoSessionContext _demoSessionContext;

    public DemoController(
        ApplicationDbContext context,
        IConfiguration configuration,
        DemoSessionContext demoSessionContext)
    {
        _context = context;
        _configuration = configuration;
        _demoSessionContext = demoSessionContext;
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

        // Demo databases can drift if they are long-lived. Ensure schema is current
        // before destructive reset/seed operations.
        await _context.Database.MigrateAsync(cancellationToken);

        await using var transaction =
            await _context.Database.BeginTransactionAsync(cancellationToken);

        async Task SafeDbStep(string stepName, Func<Task> action)
        {
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DemoReset] Step '{stepName}' skipped: {ex.Message}");
            }
        }

        await SafeDbStep("AnimalPhotos delete", () => _context.AnimalPhotos.ExecuteDeleteAsync(cancellationToken));
        await SafeDbStep("AnimalNotes delete", () => _context.AnimalNotes.ExecuteDeleteAsync(cancellationToken));
        await SafeDbStep("ClassificationRecords delete", () => _context.ClassificationRecords.ExecuteDeleteAsync(cancellationToken));
        await SafeDbStep("ShowAchievements delete", () => _context.ShowAchievements.ExecuteDeleteAsync(cancellationToken));
        await SafeDbStep("EmbryoRecords delete", () => _context.EmbryoRecords.ExecuteDeleteAsync(cancellationToken));
        await SafeDbStep("HeatEvents delete", () => _context.HeatEvents.ExecuteDeleteAsync(cancellationToken));
        await SafeDbStep("BreedingEvents delete", () => _context.BreedingEvents.ExecuteDeleteAsync(cancellationToken));
        await SafeDbStep("DryOffEvents delete", () => _context.DryOffEvents.ExecuteDeleteAsync(cancellationToken));
        await SafeDbStep("LutalyseEvents delete", () => _context.LutalyseEvents.ExecuteDeleteAsync(cancellationToken));
        await SafeDbStep("CalvingEvents delete", () => _context.CalvingEvents.ExecuteDeleteAsync(cancellationToken));
        await SafeDbStep(
            "legacy AnimalProductionSnapshots delete",
            () => _context.Database.ExecuteSqlRawAsync(
                "IF OBJECT_ID(N'[dbo].[AnimalProductionSnapshots]', N'U') IS NOT NULL DELETE FROM [dbo].[AnimalProductionSnapshots];",
                cancellationToken));
        await SafeDbStep(
            "Animals FK clear",
            () => _context.Animals.ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(a => a.DamId, (int?)null)
                    .SetProperty(a => a.SireId, (int?)null),
                cancellationToken));

        await SafeDbStep("Animals delete", () => _context.Animals.ExecuteDeleteAsync(cancellationToken));

        var utcNow = DateTime.UtcNow;
        const string seedUser = "DemoSeeder";
        var random = new Random(20260724);

        static string MakeRegistration(string prefix, int number) => $"{prefix}-{number:000}";

        int NextInt(int minInclusive, int maxExclusive) => random.Next(minInclusive, maxExclusive);

        var demoCows = new List<Animal>
        {
            new()
            {
                BarnName = "Aurora",
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
                SireName = "Pine Ridge Atlas",
                DamName = "Venture Meadow 214",
                CreatedBy = seedUser,
                UpdatedBy = seedUser
            },
            new()
            {
                BarnName = "Nova",
                RegisteredName = "Venture Nova 327",
                RegistrationNumber = MakeRegistration("DEMO", 327),
                Sex = AnimalSex.Female,
                AnimalStage = AnimalStage.Milking,
                AnimalStatus = AnimalStatus.Active,
                Breed = "Jersey",
                BirthDate = DateOnly.FromDateTime(utcNow.AddYears(-5)),
                CurrentLactation = NextInt(1, 5),
                SireName = "Oak Lane Premier",
                DamName = "Venture Willow 118",
                CreatedBy = seedUser,
                UpdatedBy = seedUser
            },
            new()
            {
                BarnName = "Daisy",
                RegisteredName = "Venture Daisy 214",
                RegistrationNumber = MakeRegistration("DEMO", 214),
                Sex = AnimalSex.Female,
                AnimalStage = AnimalStage.Dry,
                AnimalStatus = AnimalStatus.Active,
                Breed = "Holstein",
                BirthDate = DateOnly.FromDateTime(utcNow.AddYears(-3).AddMonths(-4)),
                CurrentLactation = NextInt(2, 5),
                SireName = "Maple Crest Banner",
                DamName = "Venture Rose 092",
                CreatedBy = seedUser,
                UpdatedBy = seedUser
            },
            new()
            {
                BarnName = "Clover",
                RegisteredName = "Venture Clover 198",
                RegistrationNumber = MakeRegistration("DEMO", 198),
                Sex = AnimalSex.Female,
                AnimalStage = AnimalStage.Heifer,
                AnimalStatus = AnimalStatus.Active,
                Breed = "Holstein",
                BirthDate = DateOnly.FromDateTime(utcNow.AddYears(-2)),
                SireName = "Riverbend Summit",
                DamName = "Venture Daisy 214",
                CreatedBy = seedUser,
                UpdatedBy = seedUser
            },
            new()
            {
                BarnName = "Ember",
                RegisteredName = "Venture Ember 612",
                RegistrationNumber = MakeRegistration("DEMO", 612),
                Sex = AnimalSex.Female,
                AnimalStage = AnimalStage.Milking,
                AnimalStatus = AnimalStatus.Active,
                Breed = "Ayrshire",
                BirthDate = DateOnly.FromDateTime(utcNow.AddYears(-4).AddMonths(-8)),
                CurrentLactation = NextInt(1, 4),
                SireName = "Cedar Hill Phoenix",
                DamName = "Venture Hazel 403",
                CreatedBy = seedUser,
                UpdatedBy = seedUser
            }
        };

        // A compact but complete show herd. These examples deliberately cover
        // every list/report state without burying a visitor in dozens of rows.
        demoCows.AddRange(
            new Animal
            {
                BarnName = "Maple", RegisteredName = "Maple Grove Maple 744",
                RegistrationNumber = MakeRegistration("DEMO", 744), Sex = AnimalSex.Female,
                AnimalStage = AnimalStage.Milking, AnimalStatus = AnimalStatus.Active,
                Breed = "Holstein", BirthDate = DateOnly.FromDateTime(utcNow.AddYears(-6)),
                CurrentLactation = 4, SireName = "Northstar Legend", DamName = "Maple Grove Iris",
                Notes = "Confirmed pregnant; dry-off planning example.", CreatedBy = seedUser, UpdatedBy = seedUser
            },
            new Animal
            {
                BarnName = "Juniper", RegisteredName = "Juniper Hill 608",
                RegistrationNumber = MakeRegistration("DEMO", 608), Sex = AnimalSex.Female,
                AnimalStage = AnimalStage.Milking, AnimalStatus = AnimalStatus.Active,
                Breed = "Holstein", BirthDate = DateOnly.FromDateTime(utcNow.AddYears(-3)),
                CurrentLactation = 2, SireName = "Riverbend Summit", DamName = "Maple",
                Notes = "Pregnancy check due example.", CreatedBy = seedUser, UpdatedBy = seedUser
            },
            new Animal
            {
                BarnName = "Ivy", RegisteredName = "Ivy Lane 711",
                RegistrationNumber = MakeRegistration("DEMO", 711), Sex = AnimalSex.Female,
                AnimalStage = AnimalStage.Heifer, AnimalStatus = AnimalStatus.Active,
                Breed = "Holstein", BirthDate = DateOnly.FromDateTime(utcNow.AddMonths(-18)),
                SireName = "Pine Ridge Atlas", DamName = "Aurora",
                Notes = "Bred heifer and show-string example.", IsFavorite = true,
                CreatedBy = seedUser, UpdatedBy = seedUser
            },
            new Animal
            {
                BarnName = "Poppy", RegisteredName = "Poppy Ridge 809",
                RegistrationNumber = MakeRegistration("DEMO", 809), Sex = AnimalSex.Female,
                AnimalStage = AnimalStage.Heifer, AnimalStatus = AnimalStatus.Active,
                Breed = "Jersey", BirthDate = DateOnly.FromDateTime(utcNow.AddMonths(-11)),
                SireName = "Oak Lane Premier", DamName = "Nova",
                Notes = "Youngstock example.", CreatedBy = seedUser, UpdatedBy = seedUser
            },
            new Animal
            {
                BarnName = "Willow", RegisteredName = "Willow Creek 923",
                RegistrationNumber = MakeRegistration("DEMO", 923), Sex = AnimalSex.Female,
                AnimalStage = AnimalStage.Dry, AnimalStatus = AnimalStatus.Active,
                Breed = "Holstein", BirthDate = DateOnly.FromDateTime(utcNow.AddYears(-5)),
                CurrentLactation = 3, SireName = "Nordic Chief", DamName = "Daisy",
                Notes = "Dry cow due soon.", CreatedBy = seedUser, UpdatedBy = seedUser
            },
            new Animal
            {
                BarnName = "Rosie", RegisteredName = "Rosie Red 332",
                RegistrationNumber = MakeRegistration("DEMO", 332), Sex = AnimalSex.Female,
                AnimalStage = AnimalStage.Milking, AnimalStatus = AnimalStatus.Active,
                Breed = "Red & White", BirthDate = DateOnly.FromDateTime(utcNow.AddYears(-4)),
                CurrentLactation = 2, SireName = "Cedar Hill Phoenix", DamName = "Ember",
                Notes = "Open cow ready to breed.", CreatedBy = seedUser, UpdatedBy = seedUser
            },
            new Animal
            {
                BarnName = "Breeze", RegisteredName = "Breeze 104",
                RegistrationNumber = null, Sex = AnimalSex.Female,
                AnimalStage = AnimalStage.Calf, AnimalStatus = AnimalStatus.Active,
                Breed = "Holstein", BirthDate = DateOnly.FromDateTime(utcNow.AddMonths(-4)),
                SireName = "Baxton", DamName = "Juniper",
                Notes = "Registration number pending.", CreatedBy = seedUser, UpdatedBy = seedUser
            },
            new Animal
            {
                BarnName = "Ace", RegisteredName = "Demo Ace ET",
                RegistrationNumber = MakeRegistration("DEMO", 990), Sex = AnimalSex.Male,
                AnimalStage = AnimalStage.Bull, AnimalStatus = AnimalStatus.Active,
                Breed = "Holstein", BirthDate = DateOnly.FromDateTime(utcNow.AddMonths(-9)),
                SireName = "Northstar Legend", DamName = "Aurora",
                CreatedBy = seedUser, UpdatedBy = seedUser
            },
            new Animal
            {
                BarnName = "Hazel", RegisteredName = "Hazel 417",
                RegistrationNumber = MakeRegistration("DEMO", 417), Sex = AnimalSex.Female,
                AnimalStage = AnimalStage.Milking, AnimalStatus = AnimalStatus.Sold,
                Breed = "Holstein", BirthDate = DateOnly.FromDateTime(utcNow.AddYears(-5)),
                CurrentLactation = 3, SoldDate = utcNow.AddDays(-18),
                SoldNotes = "Sold privately; $2,450.", SireName = "Nordic Chief", DamName = "Meadow",
                CreatedBy = seedUser, UpdatedBy = seedUser
            });

        _context.Animals.AddRange(demoCows);
        await _context.SaveChangesAsync(cancellationToken);

        var aurora = demoCows[0];
        var nova = demoCows[1];
        var daisy = demoCows[2];
        var clover = demoCows[3];
        var ember = demoCows[4];
        var maple = demoCows[5];
        var juniper = demoCows[6];
        var ivy = demoCows[7];
        var poppy = demoCows[8];
        var willow = demoCows[9];
        var rosie = demoCows[10];

        var demoCalf = new Animal
        {
            BarnName = "Spark",
            RegisteredName = "Venture Spark 001",
            RegistrationNumber = "DEMO-CALF-001",
            Sex = AnimalSex.Female,
            AnimalStage = AnimalStage.Calf,
            AnimalStatus = AnimalStatus.Active,
            Breed = "Holstein",
            BirthDate = DateOnly.FromDateTime(utcNow.AddDays(-2)),
            SireName = "Northstar Legend",
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
                HeatDateTime = utcNow.AddDays(-7),
                HeatStrength = HeatStrength.Normal,
                StandingHeat = true,
                Notes = "Recent heat candidate for embryo transfer",
                CreatedBy = seedUser,
                UpdatedBy = seedUser
            },
            new HeatEvent
            {
                AnimalId = rosie.AnimalId,
                HeatDateTime = utcNow.AddHours(-18),
                HeatStrength = HeatStrength.Strong,
                StandingHeat = true,
                Notes = "Ready for breeding decision.",
                CreatedBy = seedUser,
                UpdatedBy = seedUser
            },
            new HeatEvent
            {
                AnimalId = ivy.AnimalId,
                HeatDateTime = utcNow.AddDays(-6),
                HeatStrength = HeatStrength.Normal,
                StandingHeat = true,
                HasEmbryoTransfer = true,
                EmbryoImplantDate = utcNow.AddDays(1),
                Notes = "Recipient candidate shown in the ET workflow.",
                CreatedBy = seedUser,
                UpdatedBy = seedUser
            });

        _context.BreedingEvents.AddRange(
            new BreedingEvent
            {
                AnimalId = nova.AnimalId, BreedingDate = utcNow.AddDays(-30),
                SireUsed = "Oak Lane Premier", BreedingType = BreedingType.AI,
                PregnancyStatus = PregnancyStatus.Pregnant,
                PregnancyCheckDate = utcNow.AddDays(-2), PregnancyCheckDueDate = utcNow.AddDays(-2),
                ExpectedDueDate = utcNow.AddDays(250), RecommendedDryOffDate = utcNow.AddDays(220),
                CloseUpDate = utcNow.AddDays(235), Notes = "Confirmed pregnant demo breeding.",
                CreatedBy = seedUser, UpdatedBy = seedUser
            },
            new BreedingEvent
            {
                AnimalId = juniper.AnimalId, BreedingDate = utcNow.AddDays(-36),
                SireUsed = "Riverbend Summit", BreedingType = BreedingType.AI,
                PregnancyStatus = PregnancyStatus.Unconfirmed,
                PregnancyCheckDueDate = utcNow.AddDays(-1), ExpectedDueDate = utcNow.AddDays(244),
                RecommendedDryOffDate = utcNow.AddDays(214), CloseUpDate = utcNow.AddDays(229),
                Notes = "Pregnancy check due now.", CreatedBy = seedUser, UpdatedBy = seedUser
            },
            new BreedingEvent
            {
                AnimalId = ivy.AnimalId, BreedingDate = utcNow.AddDays(-28),
                SireUsed = "Pine Ridge Atlas", BreedingType = BreedingType.AI,
                PregnancyStatus = PregnancyStatus.Recheck,
                PregnancyCheckDueDate = utcNow.AddDays(7), ExpectedDueDate = utcNow.AddDays(252),
                RecommendedDryOffDate = utcNow.AddDays(222), CloseUpDate = utcNow.AddDays(237),
                Notes = "Heifer marked for recheck.", CreatedBy = seedUser, UpdatedBy = seedUser
            },
            new BreedingEvent
            {
                AnimalId = maple.AnimalId, BreedingDate = utcNow.AddDays(-170),
                SireUsed = "Northstar Legend", BreedingType = BreedingType.AI,
                PregnancyStatus = PregnancyStatus.Pregnant, PregnancyCheckDate = utcNow.AddDays(-135),
                PregnancyCheckDueDate = utcNow.AddDays(-135), ExpectedDueDate = utcNow.AddDays(110),
                RecommendedDryOffDate = utcNow.AddDays(80), CloseUpDate = utcNow.AddDays(95),
                Notes = "Confirmed pregnancy for dry-off planning.", CreatedBy = seedUser, UpdatedBy = seedUser
            },
            new BreedingEvent
            {
                AnimalId = willow.AnimalId, BreedingDate = utcNow.AddDays(-245),
                SireUsed = "Nordic Chief", BreedingType = BreedingType.AI,
                PregnancyStatus = PregnancyStatus.Pregnant, PregnancyCheckDate = utcNow.AddDays(-210),
                PregnancyCheckDueDate = utcNow.AddDays(-210), ExpectedDueDate = utcNow.AddDays(35),
                RecommendedDryOffDate = utcNow.AddDays(5), CloseUpDate = utcNow.AddDays(20),
                Notes = "Dry cow due next month.", CreatedBy = seedUser, UpdatedBy = seedUser
            });

        _context.DryOffEvents.AddRange(
            new DryOffEvent
            {
                AnimalId = ember.AnimalId, DryOffDate = utcNow.AddDays(-8),
                Reason = "Upcoming calving prep", Notes = "Demo dry-off event",
                CreatedBy = seedUser, UpdatedBy = seedUser
            },
            new DryOffEvent
            {
                AnimalId = willow.AnimalId, DryOffDate = utcNow.AddDays(-14),
                Reason = "Confirmed pregnant and approaching due date",
                Notes = "Dry cow list and due-within-60-days example.",
                CreatedBy = seedUser, UpdatedBy = seedUser
            });

        _context.LutalyseEvents.AddRange(
            new LutalyseEvent
            {
                AnimalId = clover.AnimalId, AdministrationDate = utcNow.AddDays(-5),
                ExpectedHeatWatchStart = utcNow.AddDays(-4), ExpectedHeatWatchEnd = utcNow.AddDays(-2),
                HeatObserved = true, HeatObservedDate = utcNow.AddDays(-3),
                Notes = "Heat observed after LUT.", CreatedBy = seedUser, UpdatedBy = seedUser
            },
            new LutalyseEvent
            {
                AnimalId = rosie.AnimalId, AdministrationDate = utcNow.AddDays(-2),
                ExpectedHeatWatchStart = utcNow.AddDays(1), ExpectedHeatWatchEnd = utcNow.AddDays(4),
                HeatObserved = false, Notes = "Watch window is coming up.",
                CreatedBy = seedUser, UpdatedBy = seedUser
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

        // Add comprehensive historical data across 12 months
        var heatEvents = new List<HeatEvent>();
        var breedingEvents = new List<BreedingEvent>();
        var calvingEvents = new List<CalvingEvent>();
        var dryOffEvents = new List<DryOffEvent>();
        var embryoRecords = new List<EmbryoRecord>();
        var showAchievements = new List<ShowAchievement>();
        var classificationRecords = new List<ClassificationRecord>();
        
        var siresUsed = new[] { "Nordic Chief", "Baxton O Manoman" };

        // Keep only a couple of current and historical examples.
        var historicalHeats = new[]
        {
            (AnimalId: aurora.AnimalId, MonthsAgo: 1, DayOffset: 4),
            (AnimalId: nova.AnimalId, MonthsAgo: 2, DayOffset: 8)
        };

        foreach (var item in historicalHeats)
        {
            var monthStart = new DateTime(
                utcNow.AddMonths(-item.MonthsAgo).Year,
                utcNow.AddMonths(-item.MonthsAgo).Month,
                1,
                10,
                0,
                0,
                DateTimeKind.Utc);
            var heatDate = monthStart.AddDays(item.DayOffset);

            heatEvents.Add(new HeatEvent
            {
                AnimalId = item.AnimalId,
                HeatDateTime = heatDate,
                HeatStrength = HeatStrength.Normal,
                StandingHeat = true,
                CreatedBy = seedUser,
                UpdatedBy = seedUser,
                CreatedAt = heatDate,
                UpdatedAt = heatDate
            });
        }

        // A small breeding history keeps analytics meaningful without
        // overwhelming the demo.
        foreach (var (item, index) in historicalHeats
                     .Where((_, index) => index % 2 == 1)
                     .Select((item, index) => (item, index)))
        {
            var monthStart = new DateTime(
                utcNow.AddMonths(-item.MonthsAgo).Year,
                utcNow.AddMonths(-item.MonthsAgo).Month,
                1,
                10,
                0,
                0,
                DateTimeKind.Utc);
            var breedingDate = monthStart.AddDays(item.DayOffset);

            breedingEvents.Add(new BreedingEvent
            {
                AnimalId = nova.AnimalId,
                BreedingDate = breedingDate,
                SireUsed = siresUsed[index % siresUsed.Length],
                BreedingType = BreedingType.AI,
                PregnancyStatus = PregnancyStatus.Unconfirmed,
                PregnancyCheckDueDate = breedingDate.AddDays(35),
                ExpectedDueDate = breedingDate.AddDays(280),
                RecommendedDryOffDate = breedingDate.AddDays(250),
                CreatedBy = seedUser,
                UpdatedBy = seedUser,
                CreatedAt = breedingDate,
                UpdatedAt = breedingDate
            });
        }

        // One historical example plus the current calving above.
        for (int i = 1; i < 2; i++)
        {
            var calvDate = utcNow.AddMonths(-9 + i * 3).AddDays(NextInt(0, 20));
            calvingEvents.Add(new CalvingEvent
            {
                AnimalId = aurora.AnimalId,
                CalvingDate = calvDate,
                CalfSex = CalfSex.Heifer,
                CalfBarnName = $"Demo Calf {i}",
                CalvingEase = CalvingEase.Unassisted,
                NumberOfCalves = 1,
                Twins = false,
                Stillborn = false,
                BirthWeight = Math.Round((decimal)(65 + random.NextDouble() * 30), 1),
                CreatedBy = seedUser,
                UpdatedBy = seedUser,
                CreatedAt = calvDate,
                UpdatedAt = calvDate
            });
        }

        // One historical example plus the current dry-off above.
        for (int i = 0; i < 1; i++)
        {
            dryOffEvents.Add(new DryOffEvent
            {
                AnimalId = ember.AnimalId,
                DryOffDate = utcNow.AddMonths(-6 + i * 2),
                Reason = "Calving prep",
                CreatedBy = seedUser,
                UpdatedBy = seedUser,
                CreatedAt = utcNow.AddMonths(-6 + i * 2),
                UpdatedAt = utcNow.AddMonths(-6 + i * 2)
            });
        }

        // One concise example of every embryo workflow stage.
        embryoRecords.AddRange(
            new EmbryoRecord { Code = "PRIMO-01", GroupName = "Primo x Chief", Sire = "Nordic Chief", Donor = "Primo", Mating = "Primo x Nordic Chief", Grade = "1", Status = EmbryoStatus.InStorage, StorageLocation = "Tank 1 / Cane A", CreatedAt = utcNow.AddMonths(-3), UpdatedAt = utcNow },
            new EmbryoRecord { Code = "PRIMO-02", GroupName = "Primo x Chief", Sire = "Nordic Chief", Donor = "Primo", Mating = "Primo x Nordic Chief", Grade = "1", Status = EmbryoStatus.InStorage, StorageLocation = "Tank 1 / Cane A", CreatedAt = utcNow.AddMonths(-3), UpdatedAt = utcNow },
            new EmbryoRecord { Code = "PRIMO-03", GroupName = "Primo x Chief", Sire = "Nordic Chief", Donor = "Primo", Mating = "Primo x Nordic Chief", Grade = "2", Status = EmbryoStatus.InStorage, StorageLocation = "Tank 1 / Cane A", CreatedAt = utcNow.AddMonths(-3), UpdatedAt = utcNow },
            new EmbryoRecord { Code = "ET-2026-002", GroupName = "Primo x Baxton", Sire = "Baxton", Donor = "Primo", Mating = "Primo x Baxton", Grade = "1", Status = EmbryoStatus.Assigned, RecipientAnimalId = nova.AnimalId, CreatedAt = utcNow.AddMonths(-2), UpdatedAt = utcNow },
            new EmbryoRecord { Code = "ET-2026-003", GroupName = "Aurora x Legend", Sire = "Northstar Legend", Donor = "Aurora", DonorAnimalId = aurora.AnimalId, Mating = "Aurora x Northstar Legend", Grade = "1", Status = EmbryoStatus.Implanted, RecipientAnimalId = daisy.AnimalId, ImplantDate = DateOnly.FromDateTime(utcNow.AddDays(-7)), LinkedBreedingNote = "Implanted after recorded heat.", CreatedAt = utcNow.AddDays(-7), UpdatedAt = utcNow },
            new EmbryoRecord { Code = "ET-2026-004", GroupName = "Meadow x Atlas", Sire = "Pine Ridge Atlas", Donor = "Meadow", Mating = "Meadow x Pine Ridge Atlas", Grade = "1", Status = EmbryoStatus.Successful, RecipientAnimalId = aurora.AnimalId, ImplantDate = DateOnly.FromDateTime(utcNow.AddMonths(-2)), LinkedBreedingNote = "Pregnancy confirmed.", CreatedAt = utcNow.AddMonths(-2), UpdatedAt = utcNow },
            new EmbryoRecord { Code = "ET-2026-005", GroupName = "Willow x Premier", Sire = "Oak Lane Premier", Donor = "Willow", DonorAnimalId = willow.AnimalId, Mating = "Willow x Oak Lane Premier", Grade = "2", Status = EmbryoStatus.Failed, RecipientAnimalId = clover.AnimalId, ImplantDate = DateOnly.FromDateTime(utcNow.AddMonths(-1)), FailureNotes = "Did not establish a pregnancy; recipient remains open.", CreatedAt = utcNow.AddMonths(-1), UpdatedAt = utcNow }
        );

        breedingEvents.AddRange(
            new BreedingEvent
            {
                AnimalId = aurora.AnimalId,
                BreedingDate = utcNow.AddMonths(-2),
                SireUsed = "Pine Ridge Atlas",
                BreedingType = BreedingType.EmbryoTransfer,
                PregnancyStatus = PregnancyStatus.Pregnant,
                PregnancyCheckDate = utcNow.AddMonths(-1),
                PregnancyCheckDueDate = utcNow.AddMonths(-2).AddDays(35),
                ExpectedDueDate = utcNow.AddMonths(-2).AddDays(280),
                RecommendedDryOffDate = utcNow.AddMonths(-2).AddDays(220),
                CloseUpDate = utcNow.AddMonths(-2).AddDays(259),
                Notes = "Demo confirmed embryo pregnancy.",
                CreatedBy = seedUser,
                UpdatedBy = seedUser
            },
            new BreedingEvent
            {
                AnimalId = clover.AnimalId,
                BreedingDate = utcNow.AddMonths(-1),
                SireUsed = "Oak Lane Premier",
                BreedingType = BreedingType.EmbryoTransfer,
                PregnancyStatus = PregnancyStatus.Open,
                PregnancyCheckDate = utcNow,
                PregnancyCheckDueDate = utcNow.AddMonths(-1).AddDays(35),
                Notes = "Demo embryo did not stick; recipient remains open.",
                CreatedBy = seedUser,
                UpdatedBy = seedUser
            }
        );

        // A couple of show achievements.
        showAchievements.AddRange(
            new ShowAchievement { AnimalId = aurora.AnimalId, ShowName = "State Fair", ShowDate = DateOnly.FromDateTime(utcNow.AddMonths(-6)), Placed = "Reserve Champion", Bagged = "Excellent" },
            new ShowAchievement { AnimalId = nova.AnimalId, ShowName = "Open Classic", ShowDate = DateOnly.FromDateTime(utcNow.AddMonths(-5)), Placed = "3rd Class", Bagged = "Good" }
        );

        // A couple of classification examples.
        classificationRecords.AddRange(
            new ClassificationRecord { AnimalId = aurora.AnimalId, ClassificationDate = utcNow.AddMonths(-8), Score = 91m, Baa = 109.4m, ClassificationLabel = "EX", CreatedBy = seedUser, UpdatedBy = seedUser, UpdatedAt = utcNow },
            new ClassificationRecord { AnimalId = nova.AnimalId, ClassificationDate = utcNow.AddMonths(-1), Score = 85m, Baa = 105.8m, ClassificationLabel = "VG", CreatedBy = seedUser, UpdatedBy = seedUser, UpdatedAt = utcNow }
        );

        _context.HeatEvents.AddRange(heatEvents);
        _context.BreedingEvents.AddRange(breedingEvents);
        _context.CalvingEvents.AddRange(calvingEvents);
        _context.DryOffEvents.AddRange(dryOffEvents);
        _context.EmbryoRecords.AddRange(embryoRecords);
        _context.ShowAchievements.AddRange(showAchievements);
        _context.ClassificationRecords.AddRange(classificationRecords);

        _context.AnimalNotes.Add(new AnimalNote
        {
            AnimalId = aurora.AnimalId,
            NoteDate = utcNow,
            NoteText = "Demo note: healthy and producing well.",
            NoteType = NoteType.General,
            CreatedBy = seedUser
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

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            return StatusCode(500, new DemoSeedResult
            {
                Message = $"Demo seed could not be saved: {ex.InnerException?.Message ?? ex.Message}"
            });
        }

        // Resetting starts a new 24-hour showcase window for this browser.
        var sessionId = _demoSessionContext.SessionId;
        if (sessionId != null)
        {
            var session = await _context.DemoSessions.FindAsync([sessionId], cancellationToken);
            if (session != null)
            {
                session.CreatedAt = utcNow;
                session.LastSeenAt = utcNow;
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
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

    [HttpPost("ensure")]
    public async Task<ActionResult<DemoSeedResult>> Ensure(
        CancellationToken cancellationToken)
    {
        var guardResult = ValidateDemoAccess();
        if (guardResult != null)
        {
            return guardResult;
        }

        await _context.Database.MigrateAsync(cancellationToken);

        var sessionId = _demoSessionContext.SessionId;
        if (sessionId != null)
        {
            var session = await _context.DemoSessions
                .AsNoTracking()
                .SingleOrDefaultAsync(item => item.DemoSessionId == sessionId, cancellationToken);

            if (session != null
                && DateTime.UtcNow - session.CreatedAt >= DemoSessionMaintenanceService.SessionLifetime)
            {
                return await Reset(cancellationToken);
            }
        }

        if (!await _context.Animals.AnyAsync(cancellationToken))
        {
            return await Reset(cancellationToken);
        }

        return Ok(new DemoSeedResult
        {
            Message = "Demo session is ready.",
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
