using System.ComponentModel.DataAnnotations;

namespace VentureHerdManager.Api.Models;

public class AnimalPhoto
{
    public int AnimalPhotoId { get; set; }

    public int AnimalId { get; set; }

    public Animal? Animal { get; set; }

    [Required]
    [MaxLength(1000)]
    public string PhotoUrl { get; set; } = string.Empty;

    public AnimalPhotoType PhotoType { get; set; } =
        AnimalPhotoType.General;

    public DateTime PhotoDate { get; set; } = DateTime.UtcNow;

    [MaxLength(500)]
    public string? Caption { get; set; }

    public int? RelatedEventId { get; set; }

    [MaxLength(100)]
    public string? RelatedEventType { get; set; }

    [MaxLength(200)]
    public string? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public enum AnimalPhotoType
{
    General = 0,
    Profile = 1,
    Heat = 2,
    Calving = 3,
    Calf = 4,
    Classification = 5,
    Udder = 6,
    Side = 7,
    Rear = 8,
    Feet = 9,
    Pedigree = 10
}