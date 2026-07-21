namespace VentureHerdManager.Api.Models;

public class CalvingEvent
{
    public int CalvingEventId { get; set; }

    public int AnimalId { get; set; }

    public DateTime CalvingDate { get; set; } = DateTime.Now;

    public CalfSex CalfSex { get; set; } = CalfSex.Unknown;

    public string? CalfBarnName { get; set; }

    public string? CalfRegisteredName { get; set; }

    public CalvingEase CalvingEase { get; set; } = CalvingEase.Unassisted;

    public bool Twins { get; set; }

    public bool Stillborn { get; set; }

    public string? Notes { get; set; }

    public string? CreatedBy { get; set; }
}

public enum CalfSex
{
    Unknown,
    Bull,
    Heifer
}

public enum CalvingEase
{
    Unassisted,
    EasyPull,
    HardPull,
    CSection
}