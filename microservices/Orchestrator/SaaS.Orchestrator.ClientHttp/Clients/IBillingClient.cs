using SaaS.Billing.Shared.DTOs;

namespace SaaS.Orchestrator.ClientHttp.Clients;

public interface IBillingClient
{
    Task<SubscriptionStatusDto?> GetSubscriptionStatusAsync(Guid subscriptionId, CancellationToken cancellationToken = default);
}
