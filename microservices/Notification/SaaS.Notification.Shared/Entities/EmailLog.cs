namespace SaaS.Notification.Shared.Entities;

public class EmailLog
{
    public Guid Id { get; set; }
    public Guid SubscriptionId { get; set; }
    public string RecipientEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Status { get; set; } = "pending"; // pending, sent, failed
    public DateTime CreatedAt { get; set; }
    public DateTime? SentAt { get; set; }
}
