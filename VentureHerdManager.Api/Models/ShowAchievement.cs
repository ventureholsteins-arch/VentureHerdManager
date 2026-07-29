using System.ComponentModel.DataAnnotations;

namespace VentureHerdManager.Api.Models;

public class ShowAchievement
{
    public int ShowAchievementId { get; set; }

    public int AnimalId { get; set; }

    public Animal? Animal { get; set; }

    [MaxLength(400)]
    public string? ShowName { get; set; }

    public DateOnly? ShowDate { get; set; }

    /// <summary>
    /// How the animal bagged/uddered up for the show
    /// </summary>
    [MaxLength(500)]
    public string? Bagged { get; set; }

    /// <summary>
    /// Placement — e.g. "1st Junior 2-Year-Old Cow", "Reserve Grand Champion"
    /// </summary>
    [MaxLength(500)]
    public string? Placed { get; set; }

    /// <summary>
    /// Judge comments, prep notes, weather, etc.
    /// </summary>
    [MaxLength(2000)]
    public string? Notes { get; set; }

    [MaxLength(200)]
    public string? CreatedBy { get; set; }

    [MaxLength(200)]
    public string? UpdatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
