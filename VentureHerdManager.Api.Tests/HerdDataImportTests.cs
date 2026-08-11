using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;
using VentureHerdManager.Api.Controllers;
using VentureHerdManager.Api.Data;
using VentureHerdManager.Api.DTOs;
using VentureHerdManager.Api.Models;
using VentureHerdManager.Api.Services;
using Xunit;

namespace VentureHerdManager.Api.Tests;

public sealed class HerdDataImportTests
{
    private static readonly JsonSerializerOptions WebJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

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
            CsvText = "Animal ID,Official ID,Animal Name,Sex,Birth Date,Breed,TPI,NM$,MILK,DPR,PL,TYPE FS,UDC,FLC\n37,HO840003293928967,VENTURE ALLEYOOP PAYTON,F,2024-03-01,HO,2125,-344,-30,-2.8,-3.2,0.99,-0.31,0.38"
        };

        var previewRow = Assert.Single((await service.PreviewAsync(request)).Rows);
        Assert.Equal(new DateOnly(2024, 3, 1), previewRow.BirthDate);
        Assert.Equal("HO", previewRow.Breed);
        Assert.Equal("F", previewRow.ImportedSex);
        var batch = await service.ApplyAsync(request);
        var record = Assert.Single(batch.Records);
        Assert.Equal(2125, record.Tpi); Assert.Equal(-344, record.NetMerit); Assert.Equal(-0.31m, record.UdderComposite);
        Assert.Contains("Official ID", record.RawDataJson);
    }

    [Fact]
    public async Task ZoetisAnalyticsIncludesRearUdderAndStrengthTraitsFromRawHeaders()
    {
        await using var context = CreateContext();
        var animal = new Animal { RegisteredName = "VENTURE ALLEYOOP PAYTON", RegistrationNumber = "840003293928967" };
        context.Animals.Add(animal);
        await context.SaveChangesAsync();

        var service = new HerdDataImportService(context);
        var request = new HerdDataImportRequest
        {
            Source = HerdDataSource.Zoetis,
            FileName = "core.csv",
            ReportDate = new DateOnly(2026, 8, 10),
            CsvText = "Animal ID,Official ID,Animal Name,Sex,Birth Date,Breed,TPI,NM$,RUH,RUW,SG,ST\n37,HO840003293928967,VENTURE ALLEYOOP PAYTON,F,2024-03-01,HO,2125,-344,1.8,1.2,0.7,2.1"
        };

        await service.ApplyAsync(request);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["HerdDataImport:AdminKey"] = "test-key"
            })
            .Build();
        var controller = new HerdDataController(
            service,
            context,
            new HerdDataAdminAccess(configuration),
            NullLogger<HerdDataController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        controller.ControllerContext.HttpContext.Request.Headers["X-Herd-Admin-Key"] = "test-key";

        var result = await controller.Analytics(CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(result);
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value, WebJson));
        var genomic = document.RootElement.GetProperty("genomic");
        var row = Assert.Single(genomic.EnumerateArray());

        Assert.Equal(1.8m, row.GetProperty("rearUdderHeight").GetDecimal());
        Assert.Equal(1.2m, row.GetProperty("rearUdderWidth").GetDecimal());
        Assert.Equal(0.7m, row.GetProperty("strength").GetDecimal());
        Assert.Equal(2.1m, row.GetProperty("stature").GetDecimal());
    }

    [Fact]
    public async Task ZoetisCsvAcceptsAlternateOfficialIdHeaders()
    {
        await using var context = CreateContext();
        var animal = new Animal { RegisteredName = "VENTURE ALLEYOOP PAYTON", RegistrationNumber = "840003293928967" };
        context.Animals.Add(animal);
        await context.SaveChangesAsync();

        var service = new HerdDataImportService(context);
        var request = new HerdDataImportRequest
        {
            Source = HerdDataSource.Zoetis,
            FileName = "core.csv",
            ReportDate = new DateOnly(2026, 8, 10),
            CsvText = "Animal ID,CDCB #,Animal Name,TPI,NM$\n37,HO840003293928967,VENTURE ALLEYOOP PAYTON,2125,-344"
        };

        var previewRow = Assert.Single((await service.PreviewAsync(request)).Rows);
        Assert.Equal(animal.AnimalId, previewRow.AnimalId);

        var batch = await service.ApplyAsync(request);
        var record = Assert.Single(batch.Records);
        Assert.Equal("HO840003293928967", record.OfficialId);
    }

    [Fact]
    public async Task ZoetisDuplicateReplaceKeepsSingleStoredImport()
    {
        await using var context = CreateContext();
        var animal = new Animal { RegisteredName = "VENTURE ALLEYOOP PAYTON", RegistrationNumber = "840003293928967" };
        context.Animals.Add(animal);
        await context.SaveChangesAsync();
        var service = new HerdDataImportService(context);
        var first = new HerdDataImportRequest
        {
            Source = HerdDataSource.Zoetis,
            FileName = "core-a.csv",
            ReportDate = new DateOnly(2026, 8, 10),
            CsvText = "Animal ID,Official ID,Animal Name,TPI,NM$,SG\n37,HO840003293928967,VENTURE ALLEYOOP PAYTON,2125,-344,0.7"
        };
        await service.ApplyAsync(first);

        var repeated = new HerdDataImportRequest
        {
            Source = HerdDataSource.Zoetis,
            FileName = "core-b.csv",
            ReportDate = first.ReportDate,
            CsvText = "Animal ID,Official ID,Animal Name,TPI,NM$,SG\n37,HO840003293928967,VENTURE ALLEYOOP PAYTON,2140,-300,0.9"
        };

        var preview = await service.PreviewAsync(repeated);
        Assert.True(preview.DuplicateImport);
        Assert.False(preview.ExactDuplicateFile);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ApplyAsync(repeated));
        Assert.Contains("already stored", error.Message);

        repeated.ConfirmDuplicateReplace = true;
        await service.ApplyAsync(repeated);

        Assert.Single(context.HerdDataImports);
        var stored = Assert.Single(context.AnimalDataRecords);
        Assert.Equal(2140, stored.Tpi);
        Assert.Equal(-300, stored.NetMerit);
    }

    [Fact]
    public async Task ZoetisDuplicateReplacePreservesPriorValuesWhenNewCellsAreBlank()
    {
        await using var context = CreateContext();
        var animal = new Animal { RegisteredName = "VENTURE ALLEYOOP PAYTON", RegistrationNumber = "840003293928967" };
        context.Animals.Add(animal);
        await context.SaveChangesAsync();
        var service = new HerdDataImportService(context);
        var reportDate = new DateOnly(2026, 8, 10);
        await service.ApplyAsync(new HerdDataImportRequest
        {
            Source = HerdDataSource.Zoetis, FileName = "core-a.csv", ReportDate = reportDate,
            CsvText = "Animal ID,Official ID,Animal Name,TPI,NM$,SG,HCR\n37,HO840003293928967,VENTURE ALLEYOOP PAYTON,2125,-344,0.7,1.2"
        });

        await service.ApplyAsync(new HerdDataImportRequest
        {
            Source = HerdDataSource.Zoetis, FileName = "core-b.csv", ReportDate = reportDate, ConfirmDuplicateReplace = true,
            CsvText = "Animal ID,Official ID,Animal Name,TPI,NM$,SG,HCR,CCR\n37,HO840003293928967,VENTURE ALLEYOOP PAYTON,2140,,,1.4,2.1"
        });

        Assert.Single(context.HerdDataImports);
        var stored = Assert.Single(context.AnimalDataRecords);
        Assert.Equal(2140, stored.Tpi);
        Assert.Equal(-344, stored.NetMerit);
        using var raw = JsonDocument.Parse(stored.RawDataJson);
        Assert.Equal("0.7", raw.RootElement.GetProperty("SG").GetString());
        Assert.Equal("1.4", raw.RootElement.GetProperty("HCR").GetString());
        Assert.Equal("2.1", raw.RootElement.GetProperty("CCR").GetString());
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

        var second = new HerdDataImportRequest { Source = HerdDataSource.Zoetis, FileName = "core2.csv", ReportDate = request.ReportDate.AddDays(1), CsvText = csv + "\n" };
        second.AnimalMappings["HO840003293928967"] = preserved.AnimalId;
        await service.ApplyAsync(second);
        Assert.Equal("123456789", preserved.RegistrationNumber);
        Assert.Equal("KEEP THIS NAME", preserved.RegisteredName);
    }

    [Fact]
    public async Task SameSourceAndReportDateCannotCreateDuplicateAnimalRows()
    {
        await using var context = CreateContext();
        var cow = new Animal { BarnName = "Paddy", RegistrationNumber = "145283179" };
        context.Animals.Add(cow);
        await context.SaveChangesAsync();
        var service = new HerdDataImportService(context);
        var first = new HerdDataImportRequest
        {
            Source = HerdDataSource.Pcdart, FileName = "first.csv", ReportDate = new DateOnly(2026, 8, 10),
            CsvText = "BarnName,DHIID,Milk\nPADDY,145283179,80"
        };
        await service.ApplyAsync(first);
        var repeated = new HerdDataImportRequest
        {
            Source = first.Source, FileName = "renamed.csv", ReportDate = first.ReportDate,
            CsvText = "BarnName,DHIID,Milk\r\nPADDY,145283179,80 "
        };

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ApplyAsync(repeated));
        Assert.Contains("already stored", error.Message);
        Assert.Single(context.AnimalDataRecords);

        repeated.ConfirmDuplicateReplace = true;
        await service.ApplyAsync(repeated);
        var stored = Assert.Single(context.AnimalDataRecords);
        Assert.Equal(80m, stored.Milk);
        Assert.Single(context.HerdDataImports);
    }

    private static ApplicationDbContext CreateContext()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["DemoMode:Enabled"] = "false" }).Build();
        return new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase($"herd-data-{Guid.NewGuid():N}").Options, new DemoSessionContext(new HttpContextAccessor(), configuration));
    }
}
