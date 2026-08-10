using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using VentureHerdManager.Api.Data;
using VentureHerdManager.Api.DTOs;
using VentureHerdManager.Api.Models;
using VentureHerdManager.Api.Services;
using Xunit;

namespace VentureHerdManager.Api.Tests;

public sealed class HerdDataImportTests
{
    [Fact]
    public async Task PcdartCsvStoresProductionValuesAndRememberedIdentity()
    {
        await using var context = CreateContext();
        var cow = new Animal { BarnName = "Seastorm", RegistrationNumber = "840003249920158" };
        context.Animals.Add(cow); await context.SaveChangesAsync();
        var service = new HerdDataImportService(context);
        var request = new HerdDataImportRequest
        {
            Source = HerdDataSource.Pcdart, FileName = "cows.csv", ReportDate = new DateOnly(2026, 8, 10),
            CsvText = "AgeYRMO_Ref,BarnName,DIM,Milk,LastCalv,Fat%,Pro%,DHIID\n02-11,SEASTOR,358,71.2,08/18/25,3.8,3.4,840003249920158"
        };

        var preview = await service.PreviewAsync(request);
        Assert.Equal(cow.AnimalId, preview.Rows.Single().AnimalId);
        var batch = await service.ApplyAsync(request);
        var record = Assert.Single(batch.Records);
        Assert.Equal(71.2m, record.Milk); Assert.Equal(358, record.DaysInMilk); Assert.Equal(3.8m, record.FatPercent);
        Assert.Equal(cow.AnimalId, context.AnimalIdentityMappings.Single().AnimalId);
    }

    [Fact]
    public async Task ZoetisCsvStoresGenomicValuesAndRawRow()
    {
        await using var context = CreateContext();
        var animal = new Animal { RegisteredName = "VENTURE ALLEYOOP PAYTON", RegistrationNumber = "840003293928967" };
        context.Animals.Add(animal); await context.SaveChangesAsync();
        var service = new HerdDataImportService(context);
        var request = new HerdDataImportRequest
        {
            Source = HerdDataSource.Zoetis, FileName = "core.csv", ReportDate = new DateOnly(2026, 8, 10),
            CsvText = "Animal ID,Official ID,Animal Name,TPI,NM$,MILK,DPR,PL,TYPE FS,UDC,FLC\n37,HO840003293928967,VENTURE ALLEYOOP PAYTON,2125,-344,-30,-2.8,-3.2,0.99,-0.31,0.38"
        };

        var batch = await service.ApplyAsync(request);
        var record = Assert.Single(batch.Records);
        Assert.Equal(2125, record.Tpi); Assert.Equal(-344, record.NetMerit); Assert.Equal(-0.31m, record.UdderComposite);
        Assert.Contains("Official ID", record.RawDataJson);
    }

    [Fact]
    public async Task BlankAnimalNamesDoNotBlockAnExactImportMatch()
    {
        await using var context = CreateContext();
        var exact = new Animal { BarnName = "Paddy", RegistrationNumber = "145283179" };
        context.Animals.AddRange(exact, new Animal { BarnName = "Embryo recipient" }, new Animal { RegisteredName = "" });
        await context.SaveChangesAsync();
        var service = new HerdDataImportService(context);
        var request = new HerdDataImportRequest
        {
            Source = HerdDataSource.Pcdart,
            FileName = "cows.csv",
            ReportDate = new DateOnly(2026, 8, 10),
            CsvText = "BarnName,DHIID,Milk\nPADDY,145283179,80"
        };

        var row = Assert.Single((await service.PreviewAsync(request)).Rows);
        Assert.Equal(exact.AnimalId, row.AnimalId);
        Assert.False(row.NeedsConfirmation);
        Assert.Single(row.Candidates);
    }

    [Fact]
    public async Task ConfirmedZoetisMatchFillsMissingIdentityWithoutOverwritingExistingData()
    {
        await using var context = CreateContext();
        var blank = new Animal { BarnName = "Payton" };
        var preserved = new Animal { BarnName = "Keep", RegisteredName = "KEEP THIS NAME", RegistrationNumber = "123456789" };
        context.Animals.AddRange(blank, preserved); await context.SaveChangesAsync();
        var service = new HerdDataImportService(context);
        var csv = "Animal ID,Official ID,Animal Name,TPI\n37,HO840003293928967,VENTURE ALLEYOOP PAYTON,2125";
        var request = new HerdDataImportRequest { Source = HerdDataSource.Zoetis, FileName = "core.csv", ReportDate = new DateOnly(2026, 8, 10), CsvText = csv };
        request.AnimalMappings["HO840003293928967"] = blank.AnimalId;
        await service.ApplyAsync(request);
        Assert.Equal("840003293928967", blank.RegistrationNumber);
        Assert.Equal("VENTURE ALLEYOOP PAYTON", blank.RegisteredName);

        var second = new HerdDataImportRequest { Source = HerdDataSource.Zoetis, FileName = "core2.csv", ReportDate = request.ReportDate, CsvText = csv + "\n" };
        second.AnimalMappings["HO840003293928967"] = preserved.AnimalId;
        await service.ApplyAsync(second);
        Assert.Equal("123456789", preserved.RegistrationNumber);
        Assert.Equal("KEEP THIS NAME", preserved.RegisteredName);
    }

    private static ApplicationDbContext CreateContext()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["DemoMode:Enabled"] = "false" }).Build();
        return new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase($"herd-data-{Guid.NewGuid():N}").Options, new DemoSessionContext(new HttpContextAccessor(), configuration));
    }
}
