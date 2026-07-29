using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using VentureHerdManager.Api.Data;
using VentureHerdManager.Api.Services;
using Xunit;

namespace VentureHerdManager.Api.Tests;

public sealed class NaabSireCatalogServiceTests
{
    [Fact]
    public async Task ImportsOfficialAissColumnsAndIsIdempotent()
    {
        await using var context = CreateContext();
        var service = new NaabSireCatalogService(context);
        var row = BuildAissRow(ptaMilk: 1250);

        var first = await service.ImportAsync(
            StreamFor(row),
            "Complete-AISS.txt",
            CancellationToken.None);
        var second = await service.ImportAsync(
            StreamFor(row),
            "Complete-AISS.txt",
            CancellationToken.None);

        var sire = await context.SireReferences.SingleAsync();
        Assert.Equal(1, first.Added);
        Assert.Equal(0, second.Added);
        Assert.Equal(1, second.Unchanged);
        Assert.Equal("  7HO12345".Trim(), sire.NaabCode);
        Assert.Equal("TEST BULL", sire.ShortName);
        Assert.Equal("840003123456789", sire.RegistrationNumber);
        Assert.Equal(1250, sire.PtaMilk);
        Assert.Equal(650, sire.NetMerit);
        Assert.Equal(new DateOnly(2024, 1, 2), sire.BirthDate);
    }

    [Fact]
    public async Task ReimportUpdatesChangedEvaluationWithoutDuplicatingSire()
    {
        await using var context = CreateContext();
        var service = new NaabSireCatalogService(context);

        await service.ImportAsync(
            StreamFor(BuildAissRow(ptaMilk: 1250)),
            "April-AISS.txt",
            CancellationToken.None);
        var result = await service.ImportAsync(
            StreamFor(BuildAissRow(ptaMilk: 1400)),
            "August-AISS.txt",
            CancellationToken.None);

        var sire = await context.SireReferences.SingleAsync();
        Assert.Equal(0, result.Added);
        Assert.Equal(1, result.Updated);
        Assert.Equal(1400, sire.PtaMilk);
        Assert.Equal("August-AISS.txt", sire.SourceFileName);
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
            .UseInMemoryDatabase($"naab-{Guid.NewGuid():N}")
            .Options;
        return new ApplicationDbContext(options, demoContext);
    }

    private static MemoryStream StreamFor(string text) =>
        new(Encoding.UTF8.GetBytes(text));

    private static string BuildAissRow(int ptaMilk)
    {
        var fields = Enumerable.Repeat(string.Empty, 95).ToArray();
        fields[0] = "HO";
        fields[1] = "840";
        fields[2] = "840003123456789";
        fields[3] = "0007";
        fields[4] = "007";
        fields[5] = "HO";
        fields[6] = "12345";
        fields[7] = "  7HO12345";
        fields[8] = "TEST FARM REGISTERED BULL";
        fields[9] = "HR";
        fields[16] = "95";
        fields[18] = ptaMilk.ToString();
        fields[19] = "45";
        fields[20] = "0.05";
        fields[21] = "38";
        fields[22] = "0.03";
        fields[26] = "2.72";
        fields[28] = "5.4";
        fields[30] = "1.8";
        fields[35] = "1.2";
        fields[38] = "1.0";
        fields[41] = "0.9";
        fields[44] = "650";
        fields[47] = "1.9";
        fields[50] = "2.1";
        fields[60] = "1.45";
        fields[62] = "2900";
        fields[63] = "1.20";
        fields[64] = "0.80";
        fields[89] = "20240102";
        fields[90] = "TEST BULL";
        fields[94] = "G";

        return string.Join(
            ',',
            fields.Select(value =>
                $"\"{value.Replace("\"", "\"\"")}\""));
    }
}
