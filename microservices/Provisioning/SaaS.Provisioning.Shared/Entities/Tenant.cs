namespace SaaS.Provisioning.Shared.Entities;

public class Tenant
{
    public Guid Id { get; set; }
    public Guid SubscriptionId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string Status { get; set; } = "pending"; // pending, provisioning, active, failed, deprovisioning, deprovisioned
    public string? ErrorMessage { get; set; }
    public int ProvisioningAttempts { get; set; } = 0;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ProvisionedAt { get; set; }
}
