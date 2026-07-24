namespace VentureHerdManager.Api.Models;

public class CalendarEventDto
{
    public string EventId { get; set; } = string.Empty;

    public int AnimalId { get; set; }

    public string AnimalName { get; set; } = string.Empty;

    public string? RegisteredName { get; set; }

    public string EventType { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public DateTime EventDate { get; set; }

    public bool IsOverdue { get; set; }

    public bool IsCompleted { get; set; }

    public string? Description { get; set; }
}