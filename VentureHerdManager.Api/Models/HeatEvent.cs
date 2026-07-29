using System.ComponentModel.DataAnnotations;

namespace VentureHerdManager.Api.Models;

public class HeatEvent
{
    public int HeatEventId { get; set; }

    public int AnimalId { get; set; }

    public Animal? Animal { get; set; }

    public DateTime HeatDateTime { get; set; } = DateTime.UtcNow;

    public HeatStrength HeatStrength { get; set; } = HeatStrength.Unknown;

    public bool StandingHeat { get; set; }

    public DateTime? ExpectedNextHeatStart { get; set; }

    public DateTime? ExpectedNextHeatEnd { get; set; }

    public bool HasEmbryoTransfer { get; set; }

    public DateTime? EmbryoImplantDate { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }

    [MaxLength(1000)]
    public string? PictureUrl { get; set; }

    [MaxLength(200)]
    public string? CreatedBy { get; set; }

    [MaxLength(200)]
    public string? UpdatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public enum HeatStrength
{
    Unknown = 0,
    Weak = 1,
    Normal = 2,
    Strong = 3
}