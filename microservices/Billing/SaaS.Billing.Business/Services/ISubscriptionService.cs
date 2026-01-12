using SaaS.Billing.Shared.Entities;

namespace SaaS.Billing.Business.Services;

public interface ISubscriptionService
{
    Task<Subscription> CreateSubscriptionAsync(Guid customerId, string planId, CancellationToken cancellationToken = default);
    Task<Subscription?> GetSubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken = default);
    Task<bool> CancelSubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken = default);
}
