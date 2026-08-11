namespace VentureHerdManager.Api.Models;

public sealed class SharedBaggingSchedule
{
    public int SharedBaggingScheduleId { get; set; }
    public string PublicToken { get; set; } = Guid.NewGuid().ToString("N");
    public string ShowName { get; set; } = "Show Bagging";
    public DateOnly ShowDate { get; set; }
    public string ScheduleJson { get; set; } = "[]";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<BaggingPushSubscription> Subscriptions { get; set; } = [];
}

public sealed class BaggingPushSubscription
{
    public int BaggingPushSubscriptionId { get; set; }
    public int SharedBaggingScheduleId { get; set; }
    public SharedBaggingSchedule Schedule { get; set; } = null!;
    public string Endpoint { get; set; } = string.Empty;
    public string P256dh { get; set; } = string.Empty;
    public string Auth { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class BaggingReminderDelivery
{
    public int BaggingReminderDeliveryId { get; set; }
    public int SharedBaggingScheduleId { get; set; }
    public string ReminderKey { get; set; } = string.Empty;
    public DateTime DeliveredAt { get; set; } = DateTime.UtcNow;
}
