namespace VentureHerdManager.Api.Models;

public class BreedingEvent
{
    public int BreedingEventId { get; set; }

    public int AnimalId { get; set; }

    public DateTime BreedingDate { get; set; } = DateTime.Now;

    public string SireUsed { get; set; } = "";

    public BreedingType BreedingType { get; set; } = BreedingType.AI;

    public DateTime ExpectedDueDate { get; set; }

    public DateTime PregnancyCheckDueDate { get; set; }

    public PregnancyStatus PregnancyStatus { get; set; } = PregnancyStatus.Unconfirmed;

    public string? Notes { get; set; }

    public string? CreatedBy { get; set; }
}

public enum BreedingType
{
    AI,
    Natural,
    EmbryoTransfer
}

public enum PregnancyStatus
{
    Unconfirmed,
    Pregnant,
    Open,
    Recheck,
    Aborted
}
