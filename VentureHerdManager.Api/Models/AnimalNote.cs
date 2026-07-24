using System.ComponentModel.DataAnnotations;

namespace VentureHerdManager.Api.Models;

public class AnimalNote
{
    public int AnimalNoteId { get; set; }

    public int AnimalId { get; set; }

    public Animal? Animal { get; set; }

    public DateTime NoteDate { get; set; } = DateTime.UtcNow;

    [Required]
    [MaxLength(4000)]
    public string NoteText { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? CreatedBy { get; set; }

    public NoteType NoteType { get; set; } = NoteType.General;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public enum NoteType
{
    General = 0,
    Health = 1,
    Breeding = 2,
    Calving = 3,
    Classification = 4,
    Sale = 5,
    Other = 6
}