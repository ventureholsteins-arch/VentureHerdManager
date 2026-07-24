namespace VentureHerdManager.Api.DTOs;

public class ArchiveAnimalRequest
{
    public DateTime SoldDate { get; set; } = DateTime.UtcNow;

    public string? SoldNotes { get; set; }

    public string? UpdatedBy { get; set; }
}

public class RestoreAnimalRequest
{
    public string? UpdatedBy { get; set; }
}
