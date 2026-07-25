using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VentureHerdManager.Api.Data;
using VentureHerdManager.Api.Models;

namespace VentureHerdManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnalyticsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public AnalyticsController(ApplicationDbContext context)
    {
        _context = context;
    }

    private record MonthCount(int Year, int Month, int Count);

    [HttpGet("herd-activity")]
    public async Task<IActionResult> GetHerdActivity(
        [FromQuery] int months = 12,
        CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.Today.AddMonths(-months + 1);
        var cutoffStart = new DateTime(cutoff.Year, cutoff.Month, 1);

        var calvings = (await _context.CalvingEvents
            .AsNoTracking()
            .Where(e => e.CalvingDate >= cutoffStart)
            .GroupBy(e => new { e.CalvingDate.Year, e.CalvingDate.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
            .ToListAsync(cancellationToken))
            .Select(x => new MonthCount(x.Year, x.Month, x.Count)).ToList();

        var heats = (await _context.HeatEvents
            .AsNoTracking()
            .Where(e => e.HeatDateTime >= cutoffStart)
            .GroupBy(e => new { e.HeatDateTime.Year, e.HeatDateTime.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
            .ToListAsync(cancellationToken))
            .Select(x => new MonthCount(x.Year, x.Month, x.Count)).ToList();

        var breedings = (await _context.BreedingEvents
            .AsNoTracking()
            .Where(e => e.BreedingDate >= cutoffStart)
            .GroupBy(e => new { e.BreedingDate.Year, e.BreedingDate.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
            .ToListAsync(cancellationToken))
            .Select(x => new MonthCount(x.Year, x.Month, x.Count)).ToList();

        var confirmedPregnancies = (await _context.BreedingEvents
            .AsNoTracking()
            .Where(e => e.BreedingDate >= cutoffStart &&
                        e.PregnancyStatus == PregnancyStatus.Pregnant)
            .GroupBy(e => new { e.BreedingDate.Year, e.BreedingDate.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
            .ToListAsync(cancellationToken))
            .Select(x => new MonthCount(x.Year, x.Month, x.Count)).ToList();

        var soldAnimals = (await _context.Animals
            .AsNoTracking()
            .Where(a => a.AnimalStatus == AnimalStatus.Sold &&
                        a.SoldDate.HasValue &&
                        a.SoldDate.Value >= cutoffStart)
            .GroupBy(a => new { a.SoldDate!.Value.Year, a.SoldDate!.Value.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
            .ToListAsync(cancellationToken))
            .Select(x => new MonthCount(x.Year, x.Month, x.Count)).ToList();

        var dryOffs = (await _context.DryOffEvents
            .AsNoTracking()
            .Where(e => e.DryOffDate >= cutoffStart)
            .GroupBy(e => new { e.DryOffDate.Year, e.DryOffDate.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
            .ToListAsync(cancellationToken))
            .Select(x => new MonthCount(x.Year, x.Month, x.Count)).ToList();

        static int Lookup(List<MonthCount> src, int year, int month) =>
            src.FirstOrDefault(x => x.Year == year && x.Month == month)?.Count ?? 0;

        var monthData = Enumerable.Range(0, months)
            .Select(i =>
            {
                var d = cutoffStart.AddMonths(i);
                return new
                {
                    label = d.ToString("MMM yyyy"),
                    calvings = Lookup(calvings, d.Year, d.Month),
                    heats = Lookup(heats, d.Year, d.Month),
                    breedings = Lookup(breedings, d.Year, d.Month),
                    confirmedPregnancies = Lookup(confirmedPregnancies, d.Year, d.Month),
                    soldAnimals = Lookup(soldAnimals, d.Year, d.Month),
                    dryOffs = Lookup(dryOffs, d.Year, d.Month)
                };
            })
            .ToList();

        var totalAnimals = await _context.Animals
            .CountAsync(a => a.AnimalStatus == AnimalStatus.Active, cancellationToken);

        var totalBreedingsWindow = breedings.Sum(b => b.Count);
        var totalConfirmedWindow = confirmedPregnancies.Sum(c => c.Count);
        var conceptionRate = totalBreedingsWindow > 0
            ? Math.Round((double)totalConfirmedWindow / totalBreedingsWindow * 100, 1)
            : 0.0;

        return Ok(new
        {
            months = monthData,
            totals = new
            {
                activeAnimals = totalAnimals,
                conceptionRatePct = conceptionRate,
                calvingsLast12Mo = calvings.Sum(c => c.Count),
                heatsLast12Mo = heats.Sum(h => h.Count),
                breedingsLast12Mo = breedings.Sum(b => b.Count)
            }
        });
    }

    [HttpGet("embryo-implants")]
    public async Task<IActionResult> GetEmbryoImplants(
        [FromQuery] int months = 12,
        CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.Today.AddMonths(-months + 1);
        var cutoffStart = new DateTime(cutoff.Year, cutoff.Month, 1);

        // Get monthly embryos implanted (from HeatEvents with EmbryoImplantDate)
        var implanted = (await _context.HeatEvents
            .AsNoTracking()
            .Where(e => e.HasEmbryoTransfer && e.EmbryoImplantDate.HasValue)
            .GroupBy(e => new { e.EmbryoImplantDate!.Value.Year, e.EmbryoImplantDate!.Value.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
            .ToListAsync(cancellationToken))
            .Select(x => new MonthCount(x.Year, x.Month, x.Count)).ToList();

        // Get monthly embryos marked as failed
        var failed = (await _context.EmbryoRecords
            .AsNoTracking()
            .Where(e => e.Status == EmbryoStatus.Failed && e.ImplantDate.HasValue)
            .GroupBy(e => new { e.ImplantDate!.Value.Year, e.ImplantDate!.Value.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
            .ToListAsync(cancellationToken))
            .Select(x => new MonthCount(x.Year, x.Month, x.Count)).ToList();

        // Get monthly embryos with a confirmed pregnancy outcome.
        var successfulPregnancies = (await _context.EmbryoRecords
            .AsNoTracking()
            .Where(e => e.Status == EmbryoStatus.Successful && e.ImplantDate.HasValue)
            .GroupBy(e => new { e.ImplantDate!.Value.Year, e.ImplantDate!.Value.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
            .ToListAsync(cancellationToken))
            .Select(x => new MonthCount(x.Year, x.Month, x.Count)).ToList();

        static int Lookup(List<MonthCount> src, int year, int month) =>
            src.FirstOrDefault(x => x.Year == year && x.Month == month)?.Count ?? 0;

        var monthData = Enumerable.Range(0, months)
            .Select(i =>
            {
                var d = cutoffStart.AddMonths(i);
                var implantCount = Lookup(implanted, d.Year, d.Month);
                var failedCount = Lookup(failed, d.Year, d.Month);
                var successCount = Lookup(successfulPregnancies, d.Year, d.Month);
                return new
                {
                    label = d.ToString("MMM yyyy"),
                    implanted = implantCount,
                    failed = failedCount,
                    successful = successCount
                };
            })
            .ToList();

        var totalImplanted = implanted.Sum(i => i.Count);
        var totalFailed = failed.Sum(f => f.Count);
        var totalSuccessful = successfulPregnancies.Sum(s => s.Count);
        var successRate = totalImplanted > 0
            ? Math.Round((double)totalSuccessful / totalImplanted * 100, 1)
            : 0.0;

        return Ok(new
        {
            months = monthData,
            totals = new
            {
                totalImplanted,
                totalFailed,
                totalSuccessful,
                successRatePct = successRate
            }
        });
    }
}
