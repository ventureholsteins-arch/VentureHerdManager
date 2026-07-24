namespace VentureHerdManager.Api.DTOs;

public class UpdateAnimalRequest
{
    public string? BarnName { get; set; }

    public string? RegisteredName { get; set; }

    public string? Breed { get; set; }

    public string? SireName { get; set; }

    public int? CurrentLactation { get; set; }

    public string? Notes { get; set; }

    public bool? IsFavorite { get; set; }
}
