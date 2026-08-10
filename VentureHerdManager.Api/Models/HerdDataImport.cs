using System.ComponentModel.DataAnnotations;

namespace VentureHerdManager.Api.Models;

public sealed class HerdDataImport
{
    public int HerdDataImportId { get; set; }
    public HerdDataSource Source { get; set; }
    [MaxLength(260)] public string FileName { get; set; } = string.Empty;
    [MaxLength(64)] public string FileHash { get; set; } = string.Empty;
    public DateOnly ReportDate { get; set; }
    public int RowsImported { get; set; }
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
    public ICollection<AnimalDataRecord> Records { get; set; } = [];
}

public sealed class AnimalDataRecord
{
    public int AnimalDataRecordId { get; set; }
    public int HerdDataImportId { get; set; }
    public HerdDataImport Import { get; set; } = null!;
    public int AnimalId { get; set; }
    public Animal Animal { get; set; } = null!;
    public HerdDataSource Source { get; set; }
    public DateOnly ReportDate { get; set; }
    [MaxLength(100)] public string SourceAnimalId { get; set; } = string.Empty;
    [MaxLength(200)] public string SourceAnimalName { get; set; } = string.Empty;
    [MaxLength(100)] public string? OfficialId { get; set; }
    public int? DaysInMilk { get; set; }
    public decimal? Milk { get; set; }
    public decimal? FatPercent { get; set; }
    public decimal? ProteinPercent { get; set; }
    public DateOnly? LastCalvingDate { get; set; }
    public int? Tpi { get; set; }
    public int? NetMerit { get; set; }
    public int? MilkPta { get; set; }
    public int? FatPta { get; set; }
    public int? ProteinPta { get; set; }
    public decimal? SomaticCellScore { get; set; }
    public decimal? DaughterPregnancyRate { get; set; }
    public decimal? ProductiveLife { get; set; }
    public decimal? TypeScore { get; set; }
    public decimal? UdderComposite { get; set; }
    public decimal? FeetLegsComposite { get; set; }
    public string RawDataJson { get; set; } = "{}";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class AnimalIdentityMapping
{
    public int AnimalIdentityMappingId { get; set; }
    public HerdDataSource Source { get; set; }
    [MaxLength(120)] public string SourceKey { get; set; } = string.Empty;
    [MaxLength(200)] public string SourceLabel { get; set; } = string.Empty;
    public int AnimalId { get; set; }
    public Animal Animal { get; set; } = null!;
    public DateTime ConfirmedAt { get; set; } = DateTime.UtcNow;
}

public enum HerdDataSource { Pcdart = 1, Zoetis = 2 }
