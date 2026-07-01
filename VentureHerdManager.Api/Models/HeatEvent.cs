namespace VentureHerdManager.Api.Models;

public class HeatEvent
{
    public int HeatEventId { get; set; }

    public int AnimalId { get; set; }

    public DateTime HeatDateTime { get; set; } = DateTime.Now;

    public string? Notes { get; set; }

    public string? PictureUrl { get; set; }

    public string? CreatedBy { get; set; }
}