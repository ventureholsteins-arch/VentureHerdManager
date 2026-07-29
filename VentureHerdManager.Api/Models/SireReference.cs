using System.ComponentModel.DataAnnotations;

namespace VentureHerdManager.Api.Models;

/// <summary>
/// Searchable reference data imported from an official NAAB AISS file.
/// This is reference data only; it does not replace the sire text preserved
/// on historical breeding records.
/// </summary>
public sealed class SireReference
{
    public int SireReferenceId { get; set; }

    [Required]
    [MaxLength(120)]
    public string ImportKey { get; set; } = string.Empty;

    [MaxLength(10)]
    public string? BreedCode { get; set; }

    [MaxLength(10)]
    public string? CountryCode { get; set; }

    [MaxLength(40)]
    public string? RegistrationNumber { get; set; }

    public int? ControllerNumber { get; set; }

    public int? StudCode { get; set; }

    [MaxLength(10)]
    public string? NaabBreedCode { get; set; }

    public int? BullNumber { get; set; }

    [MaxLength(30)]
    public string? NaabCode { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? ShortName { get; set; }

    [MaxLength(10)]
    public string? RegistryStatus { get; set; }

    [MaxLength(10)]
    public string? MarketingStatus { get; set; }

    public DateOnly? BirthDate { get; set; }

    public int? YieldReliability { get; set; }

    public int? PtaMilk { get; set; }

    public int? PtaFat { get; set; }

    public decimal? PtaFatPercent { get; set; }

    public int? PtaProtein { get; set; }

    public decimal? PtaProteinPercent { get; set; }

    public decimal? SomaticCellScore { get; set; }

    public decimal? ProductiveLife { get; set; }

    public decimal? DaughterPregnancyRate { get; set; }

    public decimal? HeiferConceptionRate { get; set; }

    public decimal? CowConceptionRate { get; set; }

    public decimal? Livability { get; set; }

    public int? NetMerit { get; set; }

    public decimal? SireCalvingEase { get; set; }

    public decimal? DaughterCalvingEase { get; set; }

    public decimal? PtaType { get; set; }

    public int? TotalPerformanceIndex { get; set; }

    public decimal? UdderComposite { get; set; }

    public decimal? FeetLegsComposite { get; set; }

    [MaxLength(260)]
    public string? SourceFileName { get; set; }

    [Required]
    [MaxLength(64)]
    public string SourceRowHash { get; set; } = string.Empty;

    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
