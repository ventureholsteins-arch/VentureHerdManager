using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using VentureHerdManager.Api.Data;
using VentureHerdManager.Api.Models;
using VentureHerdManager.Api.Services;
using Xunit;

namespace VentureHerdManager.Api.Tests;

public sealed class PaperRecordImportServiceTests : IAsyncLifetime
{
    private readonly string _sourceDirectory =
        Path.Combine(Path.GetTempPath(), $"paper-import-{Guid.NewGuid():N}");
    private ApplicationDbContext _context = null!;
    private PaperRecordImportService _service = null!;

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(_sourceDirectory);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DemoMode:Enabled"] = "false"
            })
            .Build();
        var demoContext = new DemoSessionContext(
            new HttpContextAccessor(),
            configuration);
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"paper-import-{Guid.NewGuid():N}")
            .Options;
        _context = new ApplicationDbContext(options, demoContext);
        _service = new PaperRecordImportService(
            _context,
            new TestEnvironment { ContentRootPath = _sourceDirectory });
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        Directory.Delete(_sourceDirectory, true);
    }

    [Fact]
    public async Task CreatesMissingRecipientAndLinksEmbryo()
    {
        WriteSources(
            [],
            [],
            ["New Heifer,2026-07-15,Seashell,Legend,Seashell x Legend,Yes,Yes,Paper section labeled Eggs"]);

        var report = await _service.ReconcileAsync(_sourceDirectory, true);

        var recipient = await _context.Animals.SingleAsync();
        var embryo = await _context.EmbryoRecords.SingleAsync();
        Assert.Equal(1, report.RecipientsCreated);
        Assert.Equal(AnimalStage.Heifer, recipient.AnimalStage);
        Assert.Null(recipient.BirthDate);
        Assert.Null(recipient.RegistrationNumber);
        Assert.Equal(recipient.AnimalId, embryo.RecipientAnimalId);
        Assert.Equal("Seashell x Legend", embryo.Mating);
        Assert.Equal("Seashell", embryo.Donor);
        Assert.Equal("Legend", embryo.Sire);
        Assert.Equal(new DateOnly(2026, 7, 15), embryo.ImplantDate);
        Assert.NotNull(embryo.BreedingEventId);
    }

    [Fact]
    public async Task SecondRunSkipsDuplicateBreedingAndEmbryo()
    {
        WriteSources(
            ["Pella,,,,Name only on paper,Yes"],
            ["Pella,2026-05-01,Image,,Breeding,"],
            ["Pella,2026-07-15,Seashell,Legend,Seashell x Legend,Yes,Yes,Paper section labeled Eggs"]);

        await _service.ReconcileAsync(_sourceDirectory, true);
        var second = await _service.ReconcileAsync(_sourceDirectory, true);

        Assert.Equal(2, await _context.BreedingEvents.CountAsync());
        Assert.Equal(1, await _context.EmbryoRecords.CountAsync());
        Assert.Equal(1, second.DuplicateBreedingsSkipped);
        Assert.Equal(1, second.DuplicateEmbryosSkipped);
    }

    [Fact]
    public async Task PreservesMultipleBreedingEventsForOneAnimal()
    {
        WriteSources(
            ["Catalina,,,,Name only on paper,Yes"],
            [
                "Catalina,2026-04-11,Venmo,,Breeding,Earlier paper record",
                "Catalina,2026-05-03,Venmo,,Breeding,Earlier paper record",
                "Catalina,2026-07-05,Eye Candy,,Breeding,Latest written record"
            ],
            []);

        var report = await _service.ReconcileAsync(_sourceDirectory, true);
        var dates = await _context.BreedingEvents
            .OrderBy(b => b.BreedingDate)
            .Select(b => b.BreedingDate.Date)
            .ToListAsync();

        Assert.Equal(3, report.BreedingsAdded);
        Assert.Equal(
            [new DateTime(2026, 4, 11), new DateTime(2026, 5, 3), new DateTime(2026, 7, 5)],
            dates);
    }

    [Fact]
    public async Task MatchesConfirmedChaChingPaperAlias()
    {
        _context.Animals.Add(new Animal
        {
            BarnName = "Cha Ching",
            RegisteredName = "VENTURE BEEMER CHA CHING",
            RegistrationNumber = "3249920156"
        });
        await _context.SaveChangesAsync();
        WriteSources(
            ["Chaching,,,,Paper spelling omits the space,Yes"],
            ["Chaching,2026-05-02,Venmo,,Breeding,"],
            []);

        var report = await _service.ReconcileAsync(_sourceDirectory, true);

        Assert.Equal(1, report.AnimalMatches);
        Assert.Equal(0, report.AnimalsCreated);
        Assert.Single(await _context.Animals.ToListAsync());
        Assert.Equal(
            "Cha Ching",
            (await _context.BreedingEvents.SingleAsync()).Animal!.BarnName);
    }

    [Fact]
    public async Task AppliesConfirmedPregnancyAndDryStates()
    {
        var casanova = new Animal { BarnName = "Casanova" };
        var missy = new Animal { BarnName = "Missy", AnimalStage = AnimalStage.Milking };
        var emmy = new Animal { BarnName = "Emmy", AnimalStage = AnimalStage.Heifer };
        var ernest = new Animal { BarnName = "Ernest", AnimalStage = AnimalStage.Milking };
        _context.Animals.AddRange(casanova, missy, emmy, ernest);
        await _context.SaveChangesAsync();
        _context.BreedingEvents.Add(new BreedingEvent
        {
            AnimalId = casanova.AnimalId,
            BreedingDate = new DateTime(2026, 2, 21),
            SireUsed = "Paldwyn",
            PregnancyStatus = PregnancyStatus.Unconfirmed
        });
        _context.BreedingEvents.Add(new BreedingEvent
        {
            AnimalId = ernest.AnimalId,
            BreedingDate = new DateTime(2026, 4, 26),
            SireUsed = "Beef",
            PregnancyStatus = PregnancyStatus.Unconfirmed
        });
        await _context.SaveChangesAsync();
        WriteSources(
            [
                "Casanova,Pregnant noted,,,PG confirmed,Yes",
                "Missy,Dry / Pregnant noted,,,Dry and pregnant confirmed,Yes",
                "Emmy,Dry noted,,,Dry and bred pending confirmation,Yes",
                "Ernest,Pregnant noted,,,PG confirmed,Yes"
            ],
            [],
            []);

        await _service.ReconcileAsync(_sourceDirectory, true);

        Assert.All(
            await _context.BreedingEvents.ToListAsync(),
            breeding => Assert.Equal(
                PregnancyStatus.Pregnant,
                breeding.PregnancyStatus));
        Assert.Equal(AnimalStage.Dry, missy.AnimalStage);
        Assert.Equal(AnimalStage.Dry, emmy.AnimalStage);
    }

    [Fact]
    public async Task RepairsCarmellaEmbryoLinkWithoutDeletingOldHistory()
    {
        var carmella = new Animal { BarnName = "Carmella", AnimalStage = AnimalStage.Heifer };
        var wrongRecipient = new Animal { BarnName = "Wrong Recipient" };
        _context.Animals.AddRange(carmella, wrongRecipient);
        await _context.SaveChangesAsync();
        var oldHistory = new BreedingEvent
        {
            AnimalId = wrongRecipient.AnimalId,
            BreedingDate = new DateTime(2026, 7, 15),
            SireUsed = "Seashell x Legend",
            BreedingType = BreedingType.EmbryoTransfer
        };
        _context.BreedingEvents.Add(oldHistory);
        await _context.SaveChangesAsync();
        var existingEmbryo = new EmbryoRecord
        {
            Donor = "Seashell",
            Sire = "Legend",
            RecipientAnimalId = carmella.AnimalId,
            ImplantDate = new DateOnly(2026, 7, 15),
            BreedingEventId = oldHistory.BreedingEventId,
            Status = EmbryoStatus.Implanted
        };
        _context.EmbryoRecords.Add(existingEmbryo);
        await _context.SaveChangesAsync();
        WriteSources(
            ["Carmella,,,,Existing recipient,Yes"],
            [],
            ["Carmella,2026-07-15,Seashell,Legend,Seashell x Legend,Yes,Yes,Paper section labeled Eggs"]);

        var report = await _service.ReconcileAsync(_sourceDirectory, true);

        Assert.Equal(1, report.DuplicateEmbryosSkipped);
        Assert.Single(report.Conflicts);
        Assert.Equal(2, await _context.BreedingEvents.CountAsync());
        Assert.Equal(
            carmella.AnimalId,
            (await _context.BreedingEvents.FindAsync(existingEmbryo.BreedingEventId))!.AnimalId);
        Assert.True(await _context.BreedingEvents.AnyAsync(
            breeding => breeding.BreedingEventId == oldHistory.BreedingEventId));
    }

    [Fact]
    public async Task FlagsAmbiguousAnimalMatchesWithoutChangingEitherAnimal()
    {
        _context.Animals.AddRange(
            new Animal { BarnName = "Cade", RegistrationNumber = "ONE" },
            new Animal { RegisteredName = "Cade", RegistrationNumber = "TWO" });
        await _context.SaveChangesAsync();
        WriteSources(
            ["Cade,,,,Ambiguous duplicate name,Yes"],
            ["Cade,2026-05-24,Master,,Breeding,"],
            []);

        var report = await _service.ReconcileAsync(_sourceDirectory, true);

        Assert.Equal(2, report.Conflicts.Count);
        Assert.Empty(await _context.BreedingEvents.ToListAsync());
        Assert.Equal(2, await _context.Animals.CountAsync());
    }

    [Fact]
    public async Task KeepsExistingValidHistoryWhenAddingMissingHistory()
    {
        var animal = new Animal { BarnName = "Azure" };
        _context.Animals.Add(animal);
        await _context.SaveChangesAsync();
        var existing = new BreedingEvent
        {
            AnimalId = animal.AnimalId,
            BreedingDate = new DateTime(2026, 3, 28),
            SireUsed = "Venmo",
            PregnancyStatus = PregnancyStatus.Open,
            Notes = "Existing valid history"
        };
        _context.BreedingEvents.Add(existing);
        await _context.SaveChangesAsync();
        WriteSources(
            ["Azure,,,,Two paper breedings,Yes"],
            [
                "Azure,2026-03-28,Venmo,,Breeding,",
                "Azure,2026-05-04,Image,,Breeding,"
            ],
            []);

        var report = await _service.ReconcileAsync(_sourceDirectory, true);
        var history = await _context.BreedingEvents
            .OrderBy(breeding => breeding.BreedingDate)
            .ToListAsync();

        Assert.Equal(1, report.DuplicateBreedingsSkipped);
        Assert.Equal(1, report.BreedingsAdded);
        Assert.Equal(2, history.Count);
        Assert.Equal(PregnancyStatus.Open, history[0].PregnancyStatus);
        Assert.Equal("Existing valid history", history[0].Notes);
    }

    [Fact]
    public async Task ExactPaperBreedingMatchStillReconcilesPregnancyStatus()
    {
        var animal = new Animal { BarnName = "Colors" };
        _context.Animals.Add(animal);
        await _context.SaveChangesAsync();
        var existing = new BreedingEvent
        {
            AnimalId = animal.AnimalId,
            BreedingDate = new DateTime(2026, 4, 15),
            SireUsed = "Venmo",
            PregnancyStatus = PregnancyStatus.Unconfirmed
        };
        _context.BreedingEvents.Add(existing);
        await _context.SaveChangesAsync();
        WriteSources(
            ["Colors,Pregnant noted,Venmo,2026-04-15,PG confirmed,Yes"],
            ["Colors,2026-04-15,Venmo,PG,Breeding,"],
            []);

        var report = await _service.ReconcileAsync(
            _sourceDirectory,
            true);

        Assert.Equal(1, report.DuplicateBreedingsSkipped);
        Assert.Equal(PregnancyStatus.Pregnant, existing.PregnancyStatus);
        Assert.Equal(
            existing.BreedingDate.AddDays(280),
            existing.ExpectedDueDate);
    }

    [Fact]
    public async Task PreservesBandiBreedingAndLinksSeparateImplantedEmbryo()
    {
        var bandi = new Animal { BarnName = "Bandi" };
        _context.Animals.Add(bandi);
        await _context.SaveChangesAsync();
        WriteSources(
            ["Bandi,,Seashell x Eye Candy,2026-04-21,Existing recipient,Yes"],
            ["Bandi,2026-04-21,Seashell x Eye Candy,,Breeding / ET?,"],
            ["Bandi,2026-07-15,Polly,Goldwyn,Polly x Goldwyn,Yes,Yes,Paper section labeled Eggs"]);

        await _service.ReconcileAsync(_sourceDirectory, true);

        var history = await _context.BreedingEvents
            .Where(breeding => breeding.AnimalId == bandi.AnimalId)
            .OrderBy(breeding => breeding.BreedingDate)
            .ToListAsync();
        var embryo = await _context.EmbryoRecords.SingleAsync();
        Assert.Equal(2, history.Count);
        Assert.Equal("Seashell x Eye Candy", history[0].SireUsed);
        Assert.Equal(BreedingType.EmbryoTransfer, history[1].BreedingType);
        Assert.Equal(history[1].BreedingEventId, embryo.BreedingEventId);
        Assert.Equal(EmbryoStatus.Implanted, embryo.Status);
    }

    [Fact]
    public async Task MatchesCinnabarPaperSpellingToConfirmedCinnabunAnimal()
    {
        _context.Animals.Add(new Animal { BarnName = "Cinnabar" });
        await _context.SaveChangesAsync();
        WriteSources(
            ["Cinnabun,,,,Owner-confirmed spelling,Yes"],
            [],
            []);

        var report = await _service.ReconcileAsync(_sourceDirectory, true);

        Assert.Equal(1, report.AnimalMatches);
        Assert.Equal(0, report.AnimalsCreated);
        Assert.Single(await _context.Animals.ToListAsync());
    }

    [Fact]
    public async Task ReusesMatchingInventoryEmbryoWhenCreatingImplant()
    {
        var bandi = new Animal { BarnName = "Bandi", AnimalStage = AnimalStage.Heifer };
        _context.Animals.Add(bandi);
        _context.EmbryoRecords.Add(new EmbryoRecord
        {
            Donor = "Polly",
            Sire = "Goldwyn",
            Mating = "Polly x Goldwyn",
            Status = EmbryoStatus.InStorage,
            Code = "PG-1"
        });
        await _context.SaveChangesAsync();
        WriteSources(
            ["Bandi,,,,Existing recipient,Yes"],
            [],
            ["Bandi,2026-07-15,Polly,Goldwyn,Polly x Goldwyn,Unconfirmed,Yes,Yes,Paper implant"]);

        var report = await _service.ReconcileAsync(_sourceDirectory, true);

        var embryo = await _context.EmbryoRecords.SingleAsync();
        Assert.Equal(0, report.EmbryosAdded);
        Assert.Equal(1, report.RecordsUpdated);
        Assert.Equal("PG-1", embryo.Code);
        Assert.Equal(bandi.AnimalId, embryo.RecipientAnimalId);
        Assert.Equal(new DateOnly(2026, 7, 15), embryo.ImplantDate);
        Assert.Equal(EmbryoStatus.Implanted, embryo.Status);
        Assert.NotNull(embryo.BreedingEventId);
    }

    [Fact]
    public async Task DidNotStickOutcomePreservesHistoryAndMarksEmbryoFailed()
    {
        WriteSources(
            ["Carmella,,,,Existing recipient,Yes"],
            [],
            ["Carmella,2026-07-15,Seashell,Legend,Seashell x Legend,Did not stick,Yes,Yes,Paper implant"]);

        await _service.ReconcileAsync(_sourceDirectory, true);

        var embryo = await _context.EmbryoRecords.SingleAsync();
        var breeding = await _context.BreedingEvents.SingleAsync();
        Assert.Equal(EmbryoStatus.Failed, embryo.Status);
        Assert.Equal(PregnancyStatus.Open, breeding.PregnancyStatus);
        Assert.Equal(embryo.BreedingEventId, breeding.BreedingEventId);
    }

    [Fact]
    public async Task LinkingEmbryoDoesNotOverwriteExistingRecipientDetails()
    {
        var recipient = new Animal
        {
            BarnName = "Bandi",
            RegisteredName = "VENTURE BANDI",
            RegistrationNumber = "BANDI-REG",
            BirthDate = new DateOnly(2024, 1, 2),
            DamName = "Known Dam",
            SireName = "Known Sire",
            AnimalStage = AnimalStage.Heifer
        };
        _context.Animals.Add(recipient);
        await _context.SaveChangesAsync();
        WriteSources(
            ["Bandi,,,,Existing recipient,Yes"],
            [],
            ["Bandi,2026-07-15,Polly,Goldwyn,Polly x Goldwyn,Yes,Yes,Paper section labeled Eggs"]);

        await _service.ReconcileAsync(_sourceDirectory, true);
        _context.ChangeTracker.Clear();
        var unchangedRecipient = await _context.Animals.SingleAsync();
        var embryo = await _context.EmbryoRecords.SingleAsync();

        Assert.Equal("VENTURE BANDI", unchangedRecipient.RegisteredName);
        Assert.Equal("BANDI-REG", unchangedRecipient.RegistrationNumber);
        Assert.Equal(new DateOnly(2024, 1, 2), unchangedRecipient.BirthDate);
        Assert.Equal("Known Dam", unchangedRecipient.DamName);
        Assert.Equal("Known Sire", unchangedRecipient.SireName);
        Assert.Equal(unchangedRecipient.AnimalId, embryo.RecipientAnimalId);
    }

    [Fact]
    public async Task ReconcilesAllProvidedPaperFilesOnAnEmptyDatabase()
    {
        var source = Path.Combine(AppContext.BaseDirectory, "paper-record-import");

        var report = await _service.ReconcileAsync(source, true);

        Assert.Equal(40, report.AnimalsCreated);
        Assert.Equal(4, report.RecipientsCreated);
        Assert.Equal(32, report.BreedingsAdded);
        Assert.Equal(6, report.EmbryosAdded);
        Assert.Equal(6, report.Conflicts.Count);
        Assert.Equal(2, report.IgnoredRows.Count);
        Assert.Equal(40, await _context.Animals.CountAsync());
        Assert.Equal(32, await _context.BreedingEvents.CountAsync());
        Assert.Equal(6, await _context.EmbryoRecords.CountAsync());
        Assert.DoesNotContain(
            await _context.Animals.ToListAsync(),
            animal => animal.BarnName == "Pixie");
        var bandi = await _context.Animals.SingleAsync(animal => animal.BarnName == "Bandi");
        var carmella = await _context.Animals.SingleAsync(animal => animal.BarnName == "Carmella");
        var seaTurtle = await _context.Animals.SingleAsync(animal => animal.BarnName == "Sea Turtle");
        Assert.Equal(AnimalStage.Milking, seaTurtle.AnimalStage);
        Assert.Contains(
            await _context.EmbryoRecords.ToListAsync(),
            embryo => embryo.RecipientAnimalId == bandi.AnimalId
                && embryo.Status == EmbryoStatus.Failed);
        Assert.Contains(
            await _context.EmbryoRecords.ToListAsync(),
            embryo => embryo.RecipientAnimalId == carmella.AnimalId
                && embryo.Status == EmbryoStatus.Failed);
    }

    [Fact]
    public async Task FullProvidedPaperImportIsIdempotent()
    {
        var source = Path.Combine(AppContext.BaseDirectory, "paper-record-import");

        await _service.ReconcileAsync(source, true);
        var second = await _service.ReconcileAsync(source, true);

        Assert.Equal(40, await _context.Animals.CountAsync());
        Assert.Equal(32, await _context.BreedingEvents.CountAsync());
        Assert.Equal(6, await _context.EmbryoRecords.CountAsync());
        Assert.Equal(0, second.AnimalsCreated);
        Assert.Equal(0, second.BreedingsAdded);
        Assert.Equal(0, second.EmbryosAdded);
    }

    private void WriteSources(
        IReadOnlyList<string> animals,
        IReadOnlyList<string> breedings,
        IReadOnlyList<string> embryos)
    {
        File.WriteAllLines(
            Path.Combine(_sourceDirectory, "animals.csv"),
            new[] { "Animal Name,Stage / Status from Notes,Current / Latest Sire,Latest Bred Date,Source Note,Needs DB Check" }
                .Concat(animals));
        File.WriteAllLines(
            Path.Combine(_sourceDirectory, "breedings.csv"),
            new[] { "Animal Name,Bred Date,Sire / Mating,Paper Status,Record Type,Notes" }
                .Concat(breedings));
        File.WriteAllLines(
            Path.Combine(_sourceDirectory, "embryos.csv"),
            new[] { "Recipient / Linked Animal,Implant / Bred Date,Embryo Dam,Embryo Sire,Mating,Outcome,Create Animal If Missing?,Create Embryo?,Review Note" }
                .Concat(embryos));
    }

    private sealed class TestEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "VentureHerdManager.Api.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = "Testing";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
