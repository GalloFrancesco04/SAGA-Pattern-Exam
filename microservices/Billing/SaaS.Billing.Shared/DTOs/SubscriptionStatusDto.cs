namespace SaaS.Billing.Shared.DTOs;

public class SubscriptionStatusDto
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PlanId { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
