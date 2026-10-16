using System.Reflection;
using VentureHerdManager.Api.Services;
using Xunit;

namespace VentureHerdManager.Api.Tests;

public class DashboardBaaCalculationTests
{
    [Fact]
    public void HerdAverage_ExcludesOnlyTheBottomTenPercentRoundedDown()
    {
        var values = Enumerable.Range(1, 13)
            .Select(value => (decimal)value)
            .ToList();

        var result = Calculate(values);

        // Thirteen records exclude one, not two: average of 2 through 13.
        Assert.Equal(7.5m, result);
    }

    [Fact]
    public void HerdAverage_WithFewerThanTenRecords_ExcludesNone()
    {
        var values = new List<decimal> { 100m, 110m, 120m };

        var result = Calculate(values);

        Assert.Equal(110m, result);
    }

    private static decimal? Calculate(List<decimal> values)
    {
        var method = typeof(DashboardService).GetMethod(
            "CalculateHerdAverageExcludingBottom10",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);
        return (decimal?)method.Invoke(null, new object[] { values });
    }
}
