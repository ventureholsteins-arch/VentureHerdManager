namespace VentureHerdManager.Api.DTOs;

public class CurrentClassificationDto
{
    public int AnimalId { get; set; }

    public decimal? Score { get; set; }

    public decimal? Baa { get; set; }

    public string? Label { get; set; }

    public DateTime? ClassificationDate { get; set; }
}
