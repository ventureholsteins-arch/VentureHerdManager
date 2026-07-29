using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using VentureHerdManager.Api.Controllers;
using VentureHerdManager.Api.Data;
using VentureHerdManager.Api.Models;
using VentureHerdManager.Api.Services;
using Xunit;

namespace VentureHerdManager.Api.Tests;

public sealed class PregnancyCheckTests
{
    [Fact]
    public async Task ConfirmingRegularBreedingSetsPregnancyAndDueDates()
    {
        await using var context = CreateContext();
        var animal = new Animal { BarnName = "Confirmed Cow" };
        context.Animals.Add(animal);
        await context.SaveChangesAsync();
        var bredDate = new DateTime(2026, 5, 1);
        var breeding = new BreedingEvent
        {
            AnimalId = animal.AnimalId,
            BreedingDate = bredDate,
            SireUsed = "Image",
            PregnancyStatus = PregnancyStatus.Unconfirmed
        };
        context.BreedingEvents.Add(breeding);
        await context.SaveChangesAsync();
        var controller = new BreedingEventsController(context);

        var result = await controller.UpdatePregnancyStatus(
            breeding.BreedingEventId,
            PregnancyStatus.Pregnant);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(PregnancyStatus.Pregnant, breeding.PregnancyStatus);
        Assert.NotNull(breeding.PregnancyCheckDate);
        Assert.Equal(bredDate.AddDays(280), breeding.ExpectedDueDate);
        Assert.Equal(bredDate.AddDays(220), breeding.RecommendedDryOffDate);
        Assert.Equal(bredDate.AddDays(259), breeding.CloseUpDate);
    }

    [Fact]
    public async Task OpenEmbryoCheckSynchronizesOutcomeAndClearsDueDates()
    {
        await using var context = CreateContext();
        var recipient = new Animal { BarnName = "Recipient" };
        context.Animals.Add(recipient);
        await context.SaveChangesAsync();
        var breeding = new BreedingEvent
        {
            AnimalId = recipient.AnimalId,
            BreedingDate = new DateTime(2026, 7, 15),
            SireUsed = "Polly x Goldwyn",
            BreedingType = BreedingType.EmbryoTransfer,
            PregnancyStatus = PregnancyStatus.Unconfirmed,
            ExpectedDueDate = new DateTime(2027, 4, 14)
        };
        context.BreedingEvents.Add(breeding);
        await context.SaveChangesAsync();
        var embryo = new EmbryoRecord
        {
            Donor = "Polly",
            Sire = "Goldwyn",
            Mating = "Polly x Goldwyn",
            RecipientAnimalId = recipient.AnimalId,
            ImplantDate = new DateOnly(2026, 7, 15),
            BreedingEventId = breeding.BreedingEventId,
            Status = EmbryoStatus.Implanted
        };
        context.EmbryoRecords.Add(embryo);
        await context.SaveChangesAsync();
        var controller = new BreedingEventsController(context);

        await controller.UpdatePregnancyStatus(
            breeding.BreedingEventId,
            PregnancyStatus.Open);

        Assert.Equal(PregnancyStatus.Open, breeding.PregnancyStatus);
        Assert.Equal(EmbryoStatus.Failed, embryo.Status);
        Assert.Null(breeding.ExpectedDueDate);
        Assert.Null(breeding.RecommendedDryOffDate);
        Assert.Null(breeding.CloseUpDate);
        Assert.NotNull(embryo.FailureNotes);
    }

    [Fact]
    public async Task EditingEmbryoTransferKeepsEmbryoSireAndMatingSeparate()
    {
        await using var context = CreateContext();
        var recipient = new Animal { BarnName = "Recipient" };
        context.Animals.Add(recipient);
        await context.SaveChangesAsync();
        var breeding = new BreedingEvent
        {
            AnimalId = recipient.AnimalId,
            BreedingDate = new DateTime(2026, 7, 15),
            SireUsed = "Polly x Goldwyn",
            BreedingType = BreedingType.EmbryoTransfer
        };
        context.BreedingEvents.Add(breeding);
        await context.SaveChangesAsync();
        var embryo = new EmbryoRecord
        {
            Donor = "Polly",
            Sire = "Goldwyn",
            Mating = "Polly x Goldwyn",
            RecipientAnimalId = recipient.AnimalId,
            ImplantDate = new DateOnly(2026, 7, 15),
            BreedingEventId = breeding.BreedingEventId,
            Status = EmbryoStatus.Implanted
        };
        context.EmbryoRecords.Add(embryo);
        await context.SaveChangesAsync();
        var controller = new BreedingEventsController(context);

        await controller.Update(
            breeding.BreedingEventId,
            new UpdateBreedingEventRequest
            {
                BreedingDate = new DateTime(2026, 7, 16),
                SireUsed = "Polly x Corrected Goldwyn",
                BreedingType = BreedingType.EmbryoTransfer,
                PregnancyStatus = PregnancyStatus.Unconfirmed
            });

        Assert.Equal("Goldwyn", embryo.Sire);
        Assert.Equal("Polly x Corrected Goldwyn", embryo.Mating);
        Assert.Equal(new DateOnly(2026, 7, 16), embryo.ImplantDate);
        Assert.Equal(
            new DateTime(2026, 7, 16).AddDays(273),
            breeding.ExpectedDueDate);
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
            .UseInMemoryDatabase($"preg-check-{Guid.NewGuid():N}")
            .Options;
        return new ApplicationDbContext(options, demoContext);
    }
}
