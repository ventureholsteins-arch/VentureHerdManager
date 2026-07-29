using System.ComponentModel.DataAnnotations;

namespace VentureHerdManager.Api.Models;

public class ClassificationRecord
{
    public int ClassificationRecordId { get; set; }

    public int AnimalId { get; set; }

    public Animal? Animal { get; set; }

    public DateTime? ClassificationDate { get; set; }

    public decimal Score { get; set; }

    public decimal? Baa { get; set; }

    public int? AgeInMonthsAtScoring { get; set; }

    [MaxLength(50)]
    public string? ClassificationLabel { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }

    [MaxLength(200)]
    public string? CreatedBy { get; set; }

    [MaxLength(200)]
    public string? UpdatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}