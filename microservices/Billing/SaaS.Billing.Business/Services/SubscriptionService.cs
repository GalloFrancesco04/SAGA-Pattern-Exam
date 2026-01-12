using Microsoft.EntityFrameworkCore;
using SaaS.Billing.Repository.Contexts;
using SaaS.Billing.Shared.Entities;

namespace SaaS.Billing.Business.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly BillingDbContext _context;

    public SubscriptionService(BillingDbContext context)
    {
        _context = context;
    }

    public async Task<Subscription> CreateSubscriptionAsync(Guid customerId, string planId, CancellationToken cancellationToken = default)
    {
        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            PlanId = planId,
            Status = "pending",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // TODO: Add to DbContext and save (next step)
        return await Task.FromResult(subscription);
    }

    public async Task<Subscription?> GetSubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        // TODO: Query from DbContext (next step)
        return await Task.FromResult<Subscription?>(null);
    }

    public async Task<bool> CancelSubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        // TODO: Update subscription status to "cancelled" (next step)
        return await Task.FromResult(false);
    }
}
