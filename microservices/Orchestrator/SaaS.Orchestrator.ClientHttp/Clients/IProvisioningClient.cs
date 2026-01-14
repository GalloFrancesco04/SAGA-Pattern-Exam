using SaaS.Provisioning.Shared.DTOs;

namespace SaaS.Orchestrator.ClientHttp.Clients;

public interface IProvisioningClient
{
    Task<TenantStatusDto?> GetTenantStatusAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
