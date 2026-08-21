using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using VentureHerdManager.Api.Controllers;
using VentureHerdManager.Api.Data;
using VentureHerdManager.Api.Models;
using VentureHerdManager.Api.Services;
using Xunit;

namespace VentureHerdManager.Api.Tests;

public sealed class ReproductiveIntegrityTests
{
    [Fact]
    public async Task ImplantAndOutcomeCreateOneConsistentLinkedHistory()
    {
        await using var context = CreateContext();
        var recipient = new Animal { BarnName = "Bandi" };
        var embryo = new EmbryoRecord
        {
            Donor = "Polly",
            Sire = "Goldwyn",
            Mating = "Polly x Goldwyn",
            Status = EmbryoStatus.InStorage
        };
        context.AddRange(recipient, embryo);
        await context.SaveChangesAsync();
        var controller = new EmbryoRecordsController(
            context,
            NullLogger<EmbryoRecordsController>.Instance);
        var implantDate = new DateOnly(2026, 7, 15);

        var implantResult = await controller.Implant(
            embryo.EmbryoRecordId,
            new ImplantEmbryoRequest
            {
                RecipientAnimalId = recipient.AnimalId,
                ImplantDate = implantDate
            });

        Assert.IsType<OkObjectResult>(implantResult);
        Assert.Equal(EmbryoStatus.Implanted, embryo.Status);
        Assert.NotNull(embryo.BreedingEventId);
        var breeding = await context.BreedingEvents.SingleAsync();
        Assert.Equal(recipient.AnimalId, breeding.AnimalId);
        Assert.Equal(
            implantDate.ToDateTime(TimeOnly.MinValue),
            breeding.BreedingDate);
        Assert.Equal("Polly x Goldwyn", breeding.SireUsed);
        Assert.Equal(BreedingType.EmbryoTransfer, breeding.BreedingType);

        var outcomeResult = await controller.RecordOutcome(
            embryo.EmbryoRecordId,
            new EmbryoOutcomeRequest { Successful = true });

        Assert.IsType<OkObjectResult>(outcomeResult);
        Assert.Equal(EmbryoStatus.Successful, embryo.Status);
        Assert.Equal(PregnancyStatus.Pregnant, breeding.PregnancyStatus);
        Assert.Equal(
            breeding.BreedingDate.AddDays(273),
            breeding.ExpectedDueDate);
        Assert.Single(await context.BreedingEvents.ToListAsync());
    }

    [Fact]
    public async Task UndoImplantPreservesTransferHistory()
    {
        await using var context = CreateContext();
        var recipient = new Animal { BarnName = "Correction Recipient" };
        var breeding = new BreedingEvent
        {
            AnimalId = 1,
            BreedingDate = new DateTime(2026, 7, 15),
            SireUsed = "Seashell x Legend",
            BreedingType = BreedingType.EmbryoTransfer,
            PregnancyStatus = PregnancyStatus.Pregnant,
            ExpectedDueDate = new DateTime(2027, 4, 14)
        };
        context.Animals.Add(recipient);
        await context.SaveChangesAsync();
        breeding.AnimalId = recipient.AnimalId;
        context.BreedingEvents.Add(breeding);
        await context.SaveChangesAsync();
        var embryo = new EmbryoRecord
        {
            Donor = "Seashell",
            Sire = "Legend",
            Mating = "Seashell x Legend",
            RecipientAnimalId = recipient.AnimalId,
            ImplantDate = new DateOnly(2026, 7, 15),
            BreedingEventId = breeding.BreedingEventId,
            Status = EmbryoStatus.Successful
        };
        context.EmbryoRecords.Add(embryo);
        await context.SaveChangesAsync();
        var controller = new EmbryoRecordsController(
            context,
            NullLogger<EmbryoRecordsController>.Instance);

        var result = await controller.UndoImplant(
            embryo.EmbryoRecordId);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(EmbryoStatus.InStorage, embryo.Status);
        Assert.Null(embryo.RecipientAnimalId);
        Assert.Null(embryo.ImplantDate);
        Assert.Null(embryo.BreedingEventId);
        var preserved = await context.BreedingEvents.SingleAsync();
        Assert.Equal(PregnancyStatus.Open, preserved.PregnancyStatus);
        Assert.Null(preserved.ExpectedDueDate);
        Assert.Contains(
            "corrected",
            preserved.Notes!,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LinkedEmbryoCannotDeleteItsBreedingHistory()
    {
        await using var context = CreateContext();
        var recipient = new Animal { BarnName = "Recipient" };
        context.Animals.Add(recipient);
        await context.SaveChangesAsync();
        var breeding = new BreedingEvent
        {
            AnimalId = recipient.AnimalId,
            BreedingDate = new DateTime(2026, 7, 15),
            SireUsed = "Dam x Sire",
            BreedingType = BreedingType.EmbryoTransfer
        };
        context.BreedingEvents.Add(breeding);
        await context.SaveChangesAsync();
        context.EmbryoRecords.Add(new EmbryoRecord
        {
            RecipientAnimalId = recipient.AnimalId,
            ImplantDate = new DateOnly(2026, 7, 15),
            BreedingEventId = breeding.BreedingEventId,
            Status = EmbryoStatus.Implanted
        });
        await context.SaveChangesAsync();
        var controller = new BreedingEventsController(
            context,
            NullLogger<BreedingEventsController>.Instance);

        var result = await controller.Delete(
            breeding.BreedingEventId);

        Assert.IsType<ConflictObjectResult>(result);
        Assert.Single(await context.BreedingEvents.ToListAsync());
        Assert.Single(await context.EmbryoRecords.ToListAsync());
    }

    [Fact]
    public async Task SavingLegacyUnlinkedImplantRepairsItsBreedingLink()
    {
        await using var context = CreateContext();
        var recipient = new Animal { BarnName = "Legacy Recipient" };
        context.Animals.Add(recipient);
        await context.SaveChangesAsync();
        var embryo = new EmbryoRecord
        {
            Donor = "Legacy Dam",
            Sire = "Legacy Sire",
            Mating = "Legacy Dam x Legacy Sire",
            RecipientAnimalId = recipient.AnimalId,
            ImplantDate = new DateOnly(2026, 6, 1),
            Status = EmbryoStatus.Implanted
        };
        context.EmbryoRecords.Add(embryo);
        await context.SaveChangesAsync();
        var controller = new EmbryoRecordsController(
            context,
            NullLogger<EmbryoRecordsController>.Instance);

        var result = await controller.Update(
            embryo.EmbryoRecordId,
            embryo);

        Assert.IsType<NoContentResult>(result);
        Assert.NotNull(embryo.BreedingEventId);
        var breeding = await context.BreedingEvents.SingleAsync();
        Assert.Equal(recipient.AnimalId, breeding.AnimalId);
        Assert.Equal("Legacy Dam x Legacy Sire", breeding.SireUsed);
        Assert.Equal(BreedingType.EmbryoTransfer, breeding.BreedingType);
    }

    [Fact]
    public async Task CurrentEventKeepsHistoryButExcludesOlderAndCalvedServices()
    {
        await using var context = CreateContext();
        var active = new Animal { BarnName = "Active" };
        var calved = new Animal { BarnName = "Calved" };
        context.Animals.AddRange(active, calved);
        await context.SaveChangesAsync();
        context.BreedingEvents.AddRange(
            new BreedingEvent
            {
                AnimalId = active.AnimalId,
                BreedingDate = new DateTime(2026, 3, 1),
                SireUsed = "Old Sire"
            },
            new BreedingEvent
            {
                AnimalId = active.AnimalId,
                BreedingDate = new DateTime(2026, 5, 1),
                SireUsed = "Current Sire"
            },
            new BreedingEvent
            {
                AnimalId = calved.AnimalId,
                BreedingDate = new DateTime(2025, 9, 1),
                SireUsed = "Completed Sire",
                PregnancyStatus = PregnancyStatus.Pregnant
            });
        context.CalvingEvents.Add(new CalvingEvent
        {
            AnimalId = calved.AnimalId,
            CalvingDate = new DateTime(2026, 6, 10)
        });
        await context.SaveChangesAsync();

        var current = await context.BreedingEvents
            .AsNoTracking()
            .CurrentReproductiveEvents(context)
            .ToListAsync();

        Assert.Single(current);
        Assert.Equal("Current Sire", current[0].SireUsed);
        Assert.Equal(3, await context.BreedingEvents.CountAsync());
    }

    [Fact]
    public async Task CalvingClosesPregnancyAndKeepsSuccessfulEmbryoOutcome()
    {
        await using var context = CreateContext();
        var cow = new Animal
        {
            BarnName = "Fresh Cow",
            AnimalStage = AnimalStage.Dry
        };
        context.Animals.Add(cow);
        await context.SaveChangesAsync();
        var breeding = new BreedingEvent
        {
            AnimalId = cow.AnimalId,
            BreedingDate = new DateTime(2025, 9, 1),
            SireUsed = "Donor x Sire",
            BreedingType = BreedingType.EmbryoTransfer,
            PregnancyStatus = PregnancyStatus.Pregnant,
            ExpectedDueDate = new DateTime(2026, 6, 1),
            RecommendedDryOffDate = new DateTime(2026, 4, 2)
        };
        context.BreedingEvents.Add(breeding);
        await context.SaveChangesAsync();
        var embryo = new EmbryoRecord
        {
            RecipientAnimalId = cow.AnimalId,
            ImplantDate = new DateOnly(2025, 9, 1),
            BreedingEventId = breeding.BreedingEventId,
            Status = EmbryoStatus.Implanted
        };
        context.EmbryoRecords.Add(embryo);
        await context.SaveChangesAsync();
        var controller = new CalvingEventsController(context);

        await controller.Create(new CreateCalvingEventRequest
        {
            AnimalId = cow.AnimalId,
            CalvingDate = new DateTime(2026, 6, 3)
        });

        Assert.Equal(AnimalStage.Milking, cow.AnimalStage);
        Assert.Equal(PregnancyStatus.Pregnant, breeding.PregnancyStatus);
        Assert.Null(breeding.ExpectedDueDate);
        Assert.Null(breeding.PregnancyCheckDueDate);
        Assert.Null(breeding.RecommendedDryOffDate);
        Assert.Null(breeding.CloseUpDate);
        Assert.Equal(EmbryoStatus.Successful, embryo.Status);
        Assert.Empty(await context.BreedingEvents
            .CurrentReproductiveEvents(context)
            .ToListAsync());
    }

    [Fact]
    public async Task NewBreedingClosesPriorServiceAndFailedEmbryo()
    {
        await using var context = CreateContext();
        var animal = new Animal { BarnName = "Bandi" };
        context.Animals.Add(animal);
        await context.SaveChangesAsync();
        var prior = new BreedingEvent
        {
            AnimalId = animal.AnimalId,
            BreedingDate = new DateTime(2026, 7, 15),
            SireUsed = "Polly x Goldwyn",
            BreedingType = BreedingType.EmbryoTransfer,
            PregnancyStatus = PregnancyStatus.Unconfirmed
        };
        context.BreedingEvents.Add(prior);
        await context.SaveChangesAsync();
        var embryo = new EmbryoRecord
        {
            Donor = "Polly",
            Sire = "Goldwyn",
            RecipientAnimalId = animal.AnimalId,
            ImplantDate = new DateOnly(2026, 7, 15),
            BreedingEventId = prior.BreedingEventId,
            Status = EmbryoStatus.Implanted
        };
        context.EmbryoRecords.Add(embryo);
        await context.SaveChangesAsync();
        var controller = new BreedingEventsController(
            context,
            NullLogger<BreedingEventsController>.Instance);

        await controller.Create(new BreedingEvent
        {
            AnimalId = animal.AnimalId,
            BreedingDate = new DateTime(2026, 8, 9),
            SireUsed = "Carissa x Braxton",
            BreedingType = BreedingType.EmbryoTransfer
        });

        Assert.Equal(PregnancyStatus.Open, prior.PregnancyStatus);
        Assert.Equal(EmbryoStatus.Failed, embryo.Status);
        Assert.Equal(2, await context.BreedingEvents.CountAsync());
    }

    [Fact]
    public async Task NewHeatClosesPriorBreeding()
    {
        await using var context = CreateContext();
        var animal = new Animal { BarnName = "Shine" };
        context.Animals.Add(animal);
        await context.SaveChangesAsync();
        var prior = new BreedingEvent
        {
            AnimalId = animal.AnimalId,
            BreedingDate = new DateTime(2026, 7, 1),
            SireUsed = "Prior service",
            PregnancyStatus = PregnancyStatus.Recheck
        };
        context.BreedingEvents.Add(prior);
        await context.SaveChangesAsync();
        var controller = new HeatEventsController(context);

        await controller.Create(new HeatEvent
        {
            AnimalId = animal.AnimalId,
            HeatDateTime = new DateTime(2026, 8, 1)
        });

        Assert.Equal(PregnancyStatus.Open, prior.PregnancyStatus);
        Assert.Null(prior.PregnancyCheckDueDate);
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
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(
                $"reproductive-integrity-{Guid.NewGuid():N}")
            .Options;
        return new ApplicationDbContext(options, demoContext);
    }
}
