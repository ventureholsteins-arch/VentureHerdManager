using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using VentureHerdManager.Api.Data;
using VentureHerdManager.Api.DTOs;
using VentureHerdManager.Api.Models;
using VentureHerdManager.Api.Services;
using Xunit;

namespace VentureHerdManager.Api.Tests;

public sealed class AnimalDetailTests
{
    [Fact]
    public async Task AnimalDetailReturnsSmallSerializableAnimal()
    {
        await using var context = CreateContext();
        var animal = new Animal
        {
            BarnName = "Detail Cow",
            DamName = "Dam",
            SireName = "Sire"
        };
        context.Animals.Add(animal);
        await context.SaveChangesAsync();

        context.HeatEvents.Add(new HeatEvent
        {
            AnimalId = animal.AnimalId,
            Animal = animal,
            HeatDateTime = new DateTime(2026, 7, 1)
        });
        context.BreedingEvents.Add(new BreedingEvent
        {
            AnimalId = animal.AnimalId,
            Animal = animal,
            BreedingDate = new DateTime(2026, 7, 2),
            SireUsed = "Test Sire"
        });
        await context.SaveChangesAsync();

        var service = new AnimalService(context);
        var loaded = await service.GetAnimalByIdAsync(animal.AnimalId);

        Assert.NotNull(loaded);
        Assert.Equal("Detail Cow", loaded.BarnName);
        Assert.Empty(loaded.HeatEvents);
        Assert.Empty(loaded.BreedingEvents);

        var json = JsonSerializer.Serialize(loaded);
        Assert.Contains("\"BarnName\":\"Detail Cow\"", json);
    }

    [Fact]
    public async Task MissingAnimalDetailReturnsNull()
    {
        await using var context = CreateContext();
        var service = new AnimalService(context);

        var loaded = await service.GetAnimalByIdAsync(999);

        Assert.Null(loaded);
    }

    [Fact]
    public async Task UpdateAnimalPersistsBirthDateAndRegistrationNumber()
    {
        await using var context = CreateContext();
        var animal = new Animal
        {
            BarnName = "Birthday Cow",
            BirthDate = new DateOnly(2024, 1, 1),
            RegistrationNumber = "OLD-REG"
        };
        context.Animals.Add(animal);
        await context.SaveChangesAsync();

        var service = new AnimalService(context);
        var updated = service.UpdateAnimal(
            animal.AnimalId,
            new UpdateAnimalRequest
            {
                BirthDate = new DateOnly(2023, 9, 14),
                RegistrationNumber = "840003123456789"
            });

        Assert.NotNull(updated);
        Assert.Equal(new DateOnly(2023, 9, 14), updated.BirthDate);
        Assert.Equal("840003123456789", updated.RegistrationNumber);

        context.ChangeTracker.Clear();
        var stored = await context.Animals.SingleAsync();
        Assert.Equal(new DateOnly(2023, 9, 14), stored.BirthDate);
        Assert.Equal("840003123456789", stored.RegistrationNumber);
    }

    [Fact]
    public async Task CreateAnimalLinksUniqueExistingDamByName()
    {
        await using var context = CreateContext();
        var dam = new Animal { BarnName = "Shila", AnimalStatus = AnimalStatus.Active };
        context.Animals.Add(dam);
        await context.SaveChangesAsync();

        var service = new AnimalService(context);
        var calf = service.CreateAnimal(new Animal
        {
            BarnName = "Status",
            DamName = " shila ",
            AnimalStage = AnimalStage.Calf,
            AnimalStatus = AnimalStatus.Active
        });

        Assert.Equal(dam.AnimalId, calf.DamId);
        Assert.Equal("shila", calf.DamName);
    }

    [Fact]
    public async Task CreateAnimalDoesNotGuessWhenDamNameIsAmbiguous()
    {
        await using var context = CreateContext();
        context.Animals.AddRange(
            new Animal { BarnName = "Clover", AnimalStatus = AnimalStatus.Active },
            new Animal { RegisteredName = "Clover", AnimalStatus = AnimalStatus.Active });
        await context.SaveChangesAsync();

        var service = new AnimalService(context);
        var calf = service.CreateAnimal(new Animal
        {
            BarnName = "Clover calf",
            DamName = "Clover",
            AnimalStage = AnimalStage.Calf,
            AnimalStatus = AnimalStatus.Active
        });

        Assert.Null(calf.DamId);
        Assert.Equal("Clover", calf.DamName);
    }

    [Fact]
    public async Task UpdateAnimalReplacesStaleDamRelationship()
    {
        await using var context = CreateContext();
        var oldDam = new Animal { BarnName = "Wrong Dam", AnimalStatus = AnimalStatus.Active };
        var correctDam = new Animal { BarnName = "Correct Dam", AnimalStatus = AnimalStatus.Active };
        var calf = new Animal { BarnName = "Test Calf", DamName = "Wrong Dam", Dam = oldDam, AnimalStatus = AnimalStatus.Active };
        context.Animals.AddRange(oldDam, correctDam, calf);
        await context.SaveChangesAsync();

        var service = new AnimalService(context);
        var updated = service.UpdateAnimal(calf.AnimalId, new UpdateAnimalRequest { DamName = "Correct Dam" });

        Assert.NotNull(updated);
        Assert.Equal(correctDam.AnimalId, updated.DamId);
        Assert.Equal("Correct Dam", updated.DamName);
    }

    private static ApplicationDbContext CreateContext()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DemoMode:Enabled"] = "false"
            })
            .Build();
        var demoContext = new DemoSessionContext(
            new HttpContextAccessor(),
            configuration);
        var options =
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(
                    $"animal-detail-{Guid.NewGuid():N}")
                .Options;

        return new ApplicationDbContext(options, demoContext);
    }
}
