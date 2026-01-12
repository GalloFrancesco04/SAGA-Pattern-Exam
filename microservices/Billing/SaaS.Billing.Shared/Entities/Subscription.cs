namespace SaaS.Billing.Shared.Entities;

public class Subscription
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string PlanId { get; set; } = string.Empty;
    public string Status { get; set; } = "pending"; // pending, active, cancelled, suspended
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
