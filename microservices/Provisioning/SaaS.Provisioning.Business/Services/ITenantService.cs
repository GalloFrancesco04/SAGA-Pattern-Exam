using SaaS.Provisioning.Shared.Entities;

namespace SaaS.Provisioning.Business.Services;

public interface ITenantService
{
    /// <summary>
    /// Provisions a new tenant for a subscription
    /// </summary>
    Task<Tenant> ProvisionTenantAsync(Guid subscriptionId, string tenantName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets tenant status by ID
    /// </summary>
    Task<Tenant?> GetTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deprovisions a tenant (cleanup resources)
    /// </summary>
    Task<bool> DeprovisionTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
