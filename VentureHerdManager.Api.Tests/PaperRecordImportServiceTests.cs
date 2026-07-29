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
    public async Task ReconcilesAllProvidedPaperFilesOnAnEmptyDatabase()
    {
        var source = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "docs", "paper-record-import"));

        var report = await _service.ReconcileAsync(source, true);

        Assert.Equal(37, report.AnimalsCreated);
        Assert.Equal(3, report.RecipientsCreated);
        Assert.Equal(30, report.BreedingsAdded);
        Assert.Equal(3, report.EmbryosAdded);
        Assert.Equal(7, report.Conflicts.Count);
        Assert.Equal(37, await _context.Animals.CountAsync());
        Assert.Equal(30, await _context.BreedingEvents.CountAsync());
        Assert.Equal(3, await _context.EmbryoRecords.CountAsync());
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
            new[] { "Recipient / Linked Animal,Implant / Bred Date,Embryo Dam,Embryo Sire,Mating,Create Animal If Missing?,Create Embryo?,Review Note" }
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
