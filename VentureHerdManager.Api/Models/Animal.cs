using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VentureHerdManager.Api.Models;

public class Animal
{
    public int AnimalId { get; set; }

    [MaxLength(100)]
    public string? BarnName { get; set; }

    [MaxLength(200)]
    public string? RegisteredName { get; set; }

    [MaxLength(100)]
    public string? RegistrationNumber { get; set; }

    public DateOnly? BirthDate { get; set; }

    public AnimalSex Sex { get; set; } = AnimalSex.Unknown;

    public AnimalStage AnimalStage { get; set; } = AnimalStage.Unknown;

    public int? CurrentLactation { get; set; }

    public AnimalStatus AnimalStatus { get; set; } = AnimalStatus.Active;

    [MaxLength(100)]
    public string? Breed { get; set; }

    public int? SireId { get; set; }

    [MaxLength(200)]
    public string? SireName { get; set; }

    public Animal? Sire { get; set; }

    public int? DamId { get; set; }

    [MaxLength(200)]
    public string? DamName { get; set; }

    public Animal? Dam { get; set; }

    [MaxLength(4000)]
    public string? Notes { get; set; }

    [MaxLength(1000)]
    public string? ProfilePictureUrl { get; set; }

    public bool IsFavorite { get; set; }

    public DateTime? SoldDate { get; set; }

    [MaxLength(2000)]
    public string? SoldNotes { get; set; }

    public DateTime? DeceasedDate { get; set; }

    [MaxLength(2000)]
    public string? DeceasedNotes { get; set; }

    [MaxLength(200)]
    public string? CreatedBy { get; set; }

    [MaxLength(200)]
    public string? UpdatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Animal> OffspringAsDam { get; set; } =
        new List<Animal>();

    public ICollection<Animal> OffspringAsSire { get; set; } =
        new List<Animal>();

    public ICollection<HeatEvent> HeatEvents { get; set; } =
        new List<HeatEvent>();

    public ICollection<BreedingEvent> BreedingEvents { get; set; } =
        new List<BreedingEvent>();

    public ICollection<CalvingEvent> CalvingEvents { get; set; } =
        new List<CalvingEvent>();

    public ICollection<DryOffEvent> DryOffEvents { get; set; } =
        new List<DryOffEvent>();

    public ICollection<AnimalNote> AnimalNotes { get; set; } =
        new List<AnimalNote>();

    public ICollection<ClassificationRecord> ClassificationRecords { get; set; } =
        new List<ClassificationRecord>();

    public ICollection<LutalyseEvent> LutalyseEvents { get; set; } =
        new List<LutalyseEvent>();

    public ICollection<AnimalPhoto> Photos { get; set; } =
        new List<AnimalPhoto>();

    [NotMapped]
    public string DisplayName =>
        !string.IsNullOrWhiteSpace(BarnName)
            ? BarnName
            : !string.IsNullOrWhiteSpace(RegisteredName)
                ? RegisteredName
                : $"Animal #{AnimalId}";

    [NotMapped]
    public bool IsActive => AnimalStatus == AnimalStatus.Active;
}

public enum AnimalSex
{
    Unknown = 0,
    Female = 1,
    Male = 2
}

public enum AnimalStage
{
    Unknown = 0,
    Calf = 1,
    Heifer = 2,
    Milking = 3,
    Dry = 4,
    Bull = 5
}

public enum AnimalStatus
{
    Active = 0,
    Sold = 1,
    Deceased = 2
}