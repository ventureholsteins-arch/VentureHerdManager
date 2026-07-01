namespace VentureHerdManager.Api.Models;

public class Animal
{
    public AnimalStatus AnimalStatus { get; set; } = AnimalStatus.Active;
    public int AnimalId { get; set; }

    public string? BarnName { get; set; }

    public string? RegisteredName { get; set; }

    public string? RegistrationNumber { get; set; }

    public DateOnly? BirthDate { get; set; }

    public AnimalSex Sex { get; set; } = AnimalSex.Unknown;

    public AnimalStage AnimalStage { get; set; } = AnimalStage.Unknown;

    public string? Breed { get; set; }

    public int? SireId { get; set; }

    public string? SireName { get; set; }

    public int? DamId { get; set; }

    public string? DamName { get; set; }

    public string? Notes { get; set; }
}

public enum AnimalSex
{
    Unknown,
    Female,
    Male
}

public enum AnimalStage
{
    Unknown,
    Calf,
    Heifer,
    Cow,
    DryCow,
    Sold,
    Deceased
}

public enum AnimalStatus
{
    Active,
    Sold,
    Deceased
}