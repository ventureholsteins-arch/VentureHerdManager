namespace VentureHerdManager.Api.Models;

public class AnimalNote
{
    public int AnimalNoteId { get; set; }

    public int AnimalId { get; set; }

    public DateTime NoteDate { get; set; } = DateTime.Now;

    public string NoteText { get; set; } = "";

    public string? CreatedBy { get; set; }
}