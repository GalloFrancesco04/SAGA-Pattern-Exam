namespace SaaS.Provisioning.Shared.DTOs;

public class TenantStatusDto
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
    public bool ReadyForUse { get; set; }
    public DateTime? ProvisionedAt { get; set; }
}
