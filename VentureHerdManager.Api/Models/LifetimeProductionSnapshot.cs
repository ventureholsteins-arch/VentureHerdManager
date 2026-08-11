using System.ComponentModel.DataAnnotations;

namespace VentureHerdManager.Api.Models;

public sealed class LifetimeProductionSnapshot
{
    public int LifetimeProductionSnapshotId { get; set; }
    public int AnimalId { get; set; }
    public Animal Animal { get; set; } = null!;
    public int HerdDataImportId { get; set; }
    public HerdDataImport Import { get; set; } = null!;
    public DateOnly ReportDate { get; set; }
    public decimal? LifetimeMilk { get; set; }
    public decimal? LifetimeFat { get; set; }
    public decimal? LifetimeProtein { get; set; }
    public int? Lactations { get; set; }
    [MaxLength(260)] public string SourceFileName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
