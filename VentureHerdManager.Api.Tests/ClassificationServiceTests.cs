using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using VentureHerdManager.Api.Data;
using VentureHerdManager.Api.Models;
using VentureHerdManager.Api.Services;
using Xunit;

namespace VentureHerdManager.Api.Tests;

public sealed class ClassificationServiceTests
{
    [Fact]
    public async Task UpdateBaaPreservesTheExistingClassificationRecord()
    {
        await using var context = CreateContext();
        var animal = new Animal { BarnName = "BAA Cow" };
        var classifiedOn = new DateTime(2026, 7, 24, 6, 46, 44, DateTimeKind.Utc);
        var record = new ClassificationRecord
        {
            Animal = animal,
            ClassificationDate = classifiedOn,
            Score = 86m,
            Baa = null,
            Notes = "Original classification note"
        };
        context.Add(record);
        await context.SaveChangesAsync();

        var service = new ClassificationService(context);
        service.UpdateBaa(record.ClassificationRecordId, 110.5m);

        var saved = await context.ClassificationRecords.SingleAsync();
        Assert.Equal(110.5m, saved.Baa);
        Assert.Equal(86m, saved.Score);
        Assert.Equal(classifiedOn, saved.ClassificationDate);
        Assert.Equal("Original classification note", saved.Notes);
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
            .UseInMemoryDatabase($"classification-{Guid.NewGuid():N}")
            .Options;
        return new ApplicationDbContext(options, demoContext);
    }
}
