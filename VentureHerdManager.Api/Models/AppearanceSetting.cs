using System.ComponentModel.DataAnnotations;

namespace VentureHerdManager.Api.Models;

public class AppearanceSetting
{
    public int AppearanceSettingId { get; set; }

    [Required]
    [MaxLength(200)]
    public string FarmName { get; set; } = "Venture Holsteins";

    [MaxLength(1000)]
    public string? LogoUrl { get; set; }

    [MaxLength(1000)]
    public string? BackgroundImageUrl { get; set; }

    public decimal BackgroundOpacity { get; set; } = 0.15m;

    public decimal OverlayOpacity { get; set; } = 0.85m;

    [MaxLength(30)]
    public string Theme { get; set; } = "light";

    [MaxLength(30)]
    public string AccentColor { get; set; } = "green";

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}