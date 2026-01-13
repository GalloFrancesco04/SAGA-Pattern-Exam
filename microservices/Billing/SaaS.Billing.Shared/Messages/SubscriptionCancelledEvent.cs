namespace SaaS.Billing.Shared.Messages;

public record SubscriptionCancelledEvent
{
    public Guid SubscriptionId { get; init; }
    public Guid CustomerId { get; init; }
    public string PlanId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime CancelledAt { get; init; }
}
