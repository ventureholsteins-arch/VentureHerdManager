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

    /// <summary>
    /// Returns 12 months of key herd activity counts for the analytics dashboard.
    /// </summary>
    [HttpGet("herd-activity")]
    public async Task<IActionResult> GetHerdActivity(
        [FromQuery] int months = 12,
        CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.Today.AddMonths(-months + 1);
        var cutoffStart = new DateTime(cutoff.Year, cutoff.Month, 1);

        // ── Calvings per month ────────────────────────────────────────────────
        var calvings = await _context.CalvingEvents
            .AsNoTracking()
            .Where(e => e.CalvingDate >= cutoffStart)
            .GroupBy(e => new { e.CalvingDate.Year, e.CalvingDate.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
            .ToListAsync(cancellationToken);

        // ── Heats per month ───────────────────────────────────────────────────
        var heats = await _context.HeatEvents
            .AsNoTracking()
            .Where(e => e.HeatDateTime >= cutoffStart)
            .GroupBy(e => new { e.HeatDateTime.Year, e.HeatDateTime.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
            .ToListAsync(cancellationToken);

        // ── Breedings per month ───────────────────────────────────────────────
        var breedings = await _context.BreedingEvents
            .AsNoTracking()
            .Where(e => e.BreedingDate >= cutoffStart)
            .GroupBy(e => new { e.BreedingDate.Year, e.BreedingDate.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
            .ToListAsync(cancellationToken);

        // ── Confirmed pregnancies per month (status = Pregnant) ───────────────
        // We use the breeding date as the "month" for this stat
        var confirmedPregnancies = await _context.BreedingEvents
            .AsNoTracking()
            .Where(e => e.BreedingDate >= cutoffStart &&
                        e.PregnancyStatus == PregnancyStatus.Pregnant)
            .GroupBy(e => new { e.BreedingDate.Year, e.BreedingDate.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
            .ToListAsync(cancellationToken);

        // ── Animals sold per month ────────────────────────────────────────────
        var soldAnimals = await _context.Animals
            .AsNoTracking()
            .Where(a => a.AnimalStatus == AnimalStatus.Sold &&
                        a.SoldDate.HasValue &&
                        a.SoldDate.Value >= cutoffStart)
            .GroupBy(a => new { a.SoldDate!.Value.Year, a.SoldDate!.Value.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
            .ToListAsync(cancellationToken);

        // ── Dry-offs per month ────────────────────────────────────────────────
        var dryOffs = await _context.DryOffEvents
            .AsNoTracking()
            .Where(e => e.DryOffDate >= cutoffStart)
            .GroupBy(e => new { e.DryOffDate.Year, e.DryOffDate.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
            .ToListAsync(cancellationToken);

        // ── Build 12-month label array ────────────────────────────────────────
        var labels = Enumerable.Range(0, months)
            .Select(i =>
            {
                var d = cutoffStart.AddMonths(i);
                return new { d.Year, d.Month, Label = d.ToString("MMM yyyy") };
            })
            .ToList();

        static int LookupCount(
            IEnumerable<dynamic> source, int year, int month) =>
            source.FirstOrDefault(x => x.Year == year && x.Month == month)?.Count ?? 0;

        var monthData = labels.Select(m => new
        {
            label = m.Label,
            calvings = LookupCount(calvings.Cast<dynamic>(), m.Year, m.Month),
            heats = LookupCount(heats.Cast<dynamic>(), m.Year, m.Month),
            breedings = LookupCount(breedings.Cast<dynamic>(), m.Year, m.Month),
            confirmedPregnancies = LookupCount(confirmedPregnancies.Cast<dynamic>(), m.Year, m.Month),
            soldAnimals = LookupCount(soldAnimals.Cast<dynamic>(), m.Year, m.Month),
            dryOffs = LookupCount(dryOffs.Cast<dynamic>(), m.Year, m.Month)
        }).ToList();

        // ── Totals ────────────────────────────────────────────────────────────
        var totalAnimals = await _context.Animals.CountAsync(
            a => a.AnimalStatus == AnimalStatus.Active, cancellationToken);

        var totalPregnant = await _context.BreedingEvents
            .AsNoTracking()
            .GroupBy(b => b.AnimalId)
            .CountAsync(cancellationToken: cancellationToken);

        // Conception rate = confirmed / total breedings (all time rolling 12 mo)
        var totalBreedingsWindow = breedings.Sum(b => b.Count);
        var totalConfirmedWindow = confirmedPregnancies.Sum(c => c.Count);
        var conceptionRate = totalBreedingsWindow > 0
            ? Math.Round((double)totalConfirmedWindow / totalBreedingsWindow * 100, 1)
            : 0;

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
}
