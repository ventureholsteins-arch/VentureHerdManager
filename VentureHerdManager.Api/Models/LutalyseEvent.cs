using System.ComponentModel.DataAnnotations;

namespace VentureHerdManager.Api.Models;

public class LutalyseEvent
{
    public int LutalyseEventId { get; set; }

    public int AnimalId { get; set; }

    public Animal? Animal { get; set; }

    public DateTime AdministrationDate { get; set; } = DateTime.UtcNow;

    public DateTime ExpectedHeatWatchStart { get; set; }

    public DateTime ExpectedHeatWatchEnd { get; set; }

    public bool HeatObserved { get; set; }

    public DateTime? HeatObservedDate { get; set; }

    public int? ResultingHeatEventId { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }

    [MaxLength(200)]
    public string? CreatedBy { get; set; }

    [MaxLength(200)]
    public string? UpdatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}