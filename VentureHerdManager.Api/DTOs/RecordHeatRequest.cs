using VentureHerdManager.Api.Models;

namespace VentureHerdManager.Api.DTOs;

public class RecordHeatRequest
{
    public DateTime HeatDateTime { get; set; } = DateTime.Now;

    public string? HeatNotes { get; set; }

    public string? PictureUrl { get; set; }

    public string? CreatedBy { get; set; }

    public bool WasBred { get; set; }

    public string? SireUsed { get; set; }

    public BreedingType? BreedingType { get; set; }

    public string? BreedingNotes { get; set; }
}