namespace SaaS.Billing.Shared.Entities;

public class TransactionalOutboxMessage
{
    public Guid Id { get; set; }
    public Guid AggregateId { get; set; } // SubscriptionId or CustomerId
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public bool IsProduced { get; set; } = false;
    public DateTime CreatedAt { get; set; }
    public DateTime? ProducedAt { get; set; }
}
