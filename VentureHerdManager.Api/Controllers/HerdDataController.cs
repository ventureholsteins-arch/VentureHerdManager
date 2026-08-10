using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VentureHerdManager.Api.Data;
using VentureHerdManager.Api.DTOs;
using VentureHerdManager.Api.Models;
using VentureHerdManager.Api.Services;

namespace VentureHerdManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class HerdDataController(HerdDataImportService importer, ApplicationDbContext context, HerdDataAdminAccess admin) : ControllerBase
{
    private IActionResult? Guard() => admin.IsAuthorized(Request) ? null : Unauthorized("Admin access is required for herd production and genomic data.");

    [HttpPost("unlock")]
    public IActionResult Unlock() => Guard() ?? Ok(new { unlocked = true });

    [HttpPost("preview")]
    public async Task<IActionResult> Preview(HerdDataImportRequest request, CancellationToken ct) => Guard() ?? Ok(await importer.PreviewAsync(request, ct));

    [HttpPost("apply")]
    public async Task<IActionResult> Apply(HerdDataImportRequest request, CancellationToken ct)
    {
        var denied = Guard(); if (denied != null) return denied;
        try
        {
            var imported = await importer.ApplyAsync(request, ct);
            return Ok(new { imported.HerdDataImportId, imported.Source, imported.FileName, imported.ReportDate, imported.RowsImported, imported.ImportedAt });
        }
        catch (InvalidOperationException exception) { return BadRequest(exception.Message); }
    }

    [HttpGet("animal/{animalId:int}")]
    public async Task<IActionResult> AnimalHistory(int animalId, CancellationToken ct)
    {
        var denied = Guard(); if (denied != null) return denied;
        return Ok(await context.AnimalDataRecords.AsNoTracking().Where(record => record.AnimalId == animalId).OrderByDescending(record => record.ReportDate).ToListAsync(ct));
    }

    [HttpGet("analytics")]
    public async Task<IActionResult> Analytics(CancellationToken ct)
    {
        var denied = Guard(); if (denied != null) return denied;
        var records = await context.AnimalDataRecords.AsNoTracking().Include(r => r.Animal).ToListAsync(ct);
        var milkDate = records.Where(r => r.Source == HerdDataSource.Pcdart).Max(r => (DateOnly?)r.ReportDate);
        var genomicDate = records.Where(r => r.Source == HerdDataSource.Zoetis).Max(r => (DateOnly?)r.ReportDate);
        var milk = records.Where(r => r.Source == HerdDataSource.Pcdart && r.ReportDate == milkDate).OrderByDescending(r => r.Milk).Select(r => new { r.AnimalId, AnimalName = r.Animal.DisplayName, r.ReportDate, r.DaysInMilk, r.Milk, r.FatPercent, r.ProteinPercent }).ToList();
        var genomic = records.Where(r => r.Source == HerdDataSource.Zoetis && r.ReportDate == genomicDate).OrderByDescending(r => r.Tpi).Select(r => new { r.AnimalId, AnimalName = r.Animal.DisplayName, r.ReportDate, r.Tpi, r.NetMerit, r.MilkPta, r.DaughterPregnancyRate, r.ProductiveLife, r.TypeScore, r.UdderComposite, r.FeetLegsComposite }).ToList();
        var combined = milk.Join(genomic, m => m.AnimalId, g => g.AnimalId, (m, g) => new { m.AnimalId, m.AnimalName, m.Milk, m.DaysInMilk, g.Tpi, g.NetMerit, g.MilkPta, g.DaughterPregnancyRate, g.ProductiveLife, g.TypeScore, g.UdderComposite, g.FeetLegsComposite }).OrderBy(x => x.Milk).ToList();
        return Ok(new { latestMilkDate = milkDate, latestGenomicDate = genomicDate, milk, genomic, combined });
    }

    [HttpGet("mating/{animalId:int}")]
    public async Task<IActionResult> Mating(int animalId, CancellationToken ct)
    {
        var denied = Guard(); if (denied != null) return denied;
        var cow = await context.AnimalDataRecords.AsNoTracking().Where(r => r.AnimalId == animalId && r.Source == HerdDataSource.Zoetis).OrderByDescending(r => r.ReportDate).FirstOrDefaultAsync(ct);
        if (cow == null) return NotFound("No genomic evaluation is stored for this animal.");
        static decimal Need(decimal? value) => Math.Max(0m, 1m - (value ?? 0m));
        var sires = await context.SireReferences.AsNoTracking().ToListAsync(ct);
        var suggestions = sires.Select(sire => new
        {
            sire.SireReferenceId, sire.Name, sire.NaabCode, sire.NetMerit, sire.PtaMilk, sire.DaughterPregnancyRate, sire.ProductiveLife, sire.PtaType, sire.UdderComposite, sire.FeetLegsComposite,
            Score = Need(cow.UdderComposite) * (sire.UdderComposite ?? 0m) + Need(cow.FeetLegsComposite) * (sire.FeetLegsComposite ?? 0m) + Need(cow.TypeScore) * (sire.PtaType ?? 0m) + Need(cow.DaughterPregnancyRate) * (sire.DaughterPregnancyRate ?? 0m) + Need(cow.ProductiveLife) * (sire.ProductiveLife ?? 0m),
            Reasons = new[] {
                (cow.UdderComposite ?? 0) < 1 && (sire.UdderComposite ?? 0) > 0 ? "Udder composite improvement" : null,
                (cow.FeetLegsComposite ?? 0) < 1 && (sire.FeetLegsComposite ?? 0) > 0 ? "Feet & legs improvement" : null,
                (cow.DaughterPregnancyRate ?? 0) < 0 && (sire.DaughterPregnancyRate ?? 0) > 0 ? "Fertility improvement" : null,
                (cow.ProductiveLife ?? 0) < 0 && (sire.ProductiveLife ?? 0) > 0 ? "Productive-life improvement" : null
            }.Where(reason => reason != null)
        }).OrderByDescending(item => item.Score).ThenByDescending(item => item.NetMerit).Take(20).ToList();
        return Ok(new { cow = new { cow.AnimalId, cow.ReportDate, cow.Tpi, cow.NetMerit, cow.MilkPta, cow.DaughterPregnancyRate, cow.ProductiveLife, cow.TypeScore, cow.UdderComposite, cow.FeetLegsComposite }, suggestions });
    }
}
