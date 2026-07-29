using System.ComponentModel.DataAnnotations;

namespace VentureHerdManager.Api.Models;

public class CalvingEvent
{
    public int CalvingEventId { get; set; }

    public int AnimalId { get; set; }

    public Animal? Animal { get; set; }

    public DateTime CalvingDate { get; set; } = DateTime.UtcNow;

    public CalfSex CalfSex { get; set; } = CalfSex.Unknown;

    [MaxLength(100)]
    public string? CalfBarnName { get; set; }

    [MaxLength(200)]
    public string? CalfRegisteredName { get; set; }

    [MaxLength(100)]
    public string? CalfRegistrationNumber { get; set; }

    public int? CalfAnimalId { get; set; }

    public Animal? CalfAnimal { get; set; }

    public CalvingEase CalvingEase { get; set; } =
        CalvingEase.Unassisted;

    public bool Twins { get; set; }

    public int NumberOfCalves { get; set; } = 1;

    public bool Stillborn { get; set; }

    public decimal? BirthWeight { get; set; }

    [MaxLength(1000)]
    public string? PictureUrl { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }

    [MaxLength(200)]
    public string? CreatedBy { get; set; }

    [MaxLength(200)]
    public string? UpdatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public enum CalfSex
{
    Unknown = 0,
    Bull = 1,
    Heifer = 2
}

public enum CalvingEase
{
    Unassisted = 0,
    EasyPull = 1,
    HardPull = 2,
    CSection = 3
}