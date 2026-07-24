using System.ComponentModel.DataAnnotations;

namespace VentureHerdManager.Api.Models;

public class BreedingEvent
{
    public int BreedingEventId { get; set; }

    public int AnimalId { get; set; }

    public Animal? Animal { get; set; }

    public DateTime BreedingDate { get; set; } = DateTime.UtcNow;

    [Required]
    [MaxLength(200)]
    public string SireUsed { get; set; } = string.Empty;

    public BreedingType BreedingType { get; set; } = BreedingType.AI;

    public DateTime? ExpectedDueDate { get; set; }

    public DateTime? PregnancyCheckDueDate { get; set; }

    public DateTime? RecommendedDryOffDate { get; set; }

    public DateTime? CloseUpDate { get; set; }

    public PregnancyStatus PregnancyStatus { get; set; } =
        PregnancyStatus.Unconfirmed;

    public DateTime? PregnancyCheckDate { get; set; }

    [MaxLength(200)]
    public string? Technician { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }

    [MaxLength(200)]
    public string? CreatedBy { get; set; }

    [MaxLength(200)]
    public string? UpdatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public enum BreedingType
{
    AI = 0,
    Natural = 1,
    EmbryoTransfer = 2
}

public enum PregnancyStatus
{
    Unconfirmed = 0,
    Pregnant = 1,
    Open = 2,
    Recheck = 3,
    Aborted = 4
}