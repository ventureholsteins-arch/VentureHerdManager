namespace VentureHerdManager.Api.Models;

public class DryOffEvent
{
    public int DryOffEventId { get; set; }

    public int AnimalId { get; set; }

    public DateTime DryOffDate { get; set; } = DateTime.Now;

    public string? Reason { get; set; }

    public string? Notes { get; set; }

    public string? CreatedBy { get; set; }
}