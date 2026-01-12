namespace SaaS.Provisioning.Shared.Entities;

public class Tenant
{
    public Guid Id { get; set; }
    public Guid SubscriptionId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string Status { get; set; } = "pending"; // pending, provisioned, failed, deprovisioned
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
