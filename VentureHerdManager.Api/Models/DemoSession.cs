using System.ComponentModel.DataAnnotations;

namespace VentureHerdManager.Api.Models;

public class DemoSession
{
    [Key]
    [MaxLength(64)]
    public string DemoSessionId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
}
