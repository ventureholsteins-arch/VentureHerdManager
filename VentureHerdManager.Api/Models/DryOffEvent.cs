using System.ComponentModel.DataAnnotations;

namespace VentureHerdManager.Api.Models;

public class DryOffEvent
{
    public int DryOffEventId { get; set; }

    public int AnimalId { get; set; }

    public Animal? Animal { get; set; }

    public DateTime DryOffDate { get; set; } = DateTime.UtcNow;

    [MaxLength(500)]
    public string? Reason { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }

    [MaxLength(200)]
    public string? CreatedBy { get; set; }

    [MaxLength(200)]
    public string? UpdatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}