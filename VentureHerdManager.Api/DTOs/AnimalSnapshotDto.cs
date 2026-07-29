using VentureHerdManager.Api.Models;

namespace VentureHerdManager.Api.DTOs;

public class AnimalSnapshotDto
{
    public Animal Animal { get; set; } = new();

    public HeatEvent? LatestHeatEvent { get; set; }

    public BreedingEvent? LatestBreedingEvent { get; set; }

    public CalvingEvent? LatestCalvingEvent { get; set; }

    public DryOffEvent? LatestDryOffEvent { get; set; }

    public ClassificationRecord? LatestClassificationRecord { get; set; }

    public List<AnimalPhoto> Photos { get; set; } = new();

    public List<AnimalTimelineEntryDto> Timeline { get; set; } = new();
}

public class AnimalTimelineEntryDto
{
    public int EventId { get; set; }

    public string EventType { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public DateTime EventDate { get; set; }

    public string? Notes { get; set; }

    public string? PhotoUrl { get; set; }
}
