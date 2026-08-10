using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using VentureHerdManager.Api.Data;
using VentureHerdManager.Api.DTOs;
using VentureHerdManager.Api.Models;
using VentureHerdManager.Api.Services;
using Xunit;

namespace VentureHerdManager.Api.Tests;

public sealed class PcdartImportTests
{
    [Fact]
    public async Task ConfirmedMappingUsesExistingAnimalAndNeverCreatesDuplicate()
    {
        await using var context = CreateContext();
        var animal = new Animal
        {
            BarnName = "Sweet Caroline Full Name",
            AnimalStage = AnimalStage.Milking
        };
        context.Animals.Add(animal);
        await context.SaveChangesAsync();
        var service = new PcdartImportService(context);
        const string shortenedName = "SWEETCAR";

        var result = await service.ImportAsync(
            new PcdartImportRequest
            {
                RawText = $"03-04 {shortenedName} 72.4 lbs milk",
                ReportLabel = "August test",
                AnimalMappings = new Dictionary<string, int>
                {
                    [shortenedName] = animal.AnimalId
                }
            },
            true);

        Assert.Equal(1, result.AnimalsMatched);
        Assert.Equal(0, result.AnimalsCreated);
        Assert.Equal(1, result.NotesCreated);
        Assert.Single(context.Animals);
        Assert.Contains("72.4 lbs milk", context.AnimalNotes.Single().NoteText);
    }

    [Fact]
    public async Task UnconfirmedNameDoesNotCreateAnimal()
    {
        await using var context = CreateContext();
        var service = new PcdartImportService(context);

        var result = await service.ImportAsync(
            new PcdartImportRequest
            {
                RawText = "03-04 UNKNOWN 44.0 lbs milk"
            },
            true);

        Assert.Contains("UNKNOWN", result.MissingAnimals);
        Assert.Empty(context.Animals);
        Assert.Empty(context.AnimalNotes);
    }

    private static ApplicationDbContext CreateContext()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DemoMode:Enabled"] = "false"
            })
            .Build();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"pcdart-import-{Guid.NewGuid():N}")
            .Options;
        return new ApplicationDbContext(
            options,
            new DemoSessionContext(new HttpContextAccessor(), configuration));
    }
}
