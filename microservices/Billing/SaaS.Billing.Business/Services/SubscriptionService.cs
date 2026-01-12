using System.Text.Json;
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
            Status = "active",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var outboxMessage = new TransactionalOutboxMessage
        {
            Id = Guid.NewGuid(),
            AggregateId = subscription.Id,
            EventType = "SubscriptionCreated",
            Payload = JsonSerializer.Serialize(subscription),
            CreatedAt = DateTime.UtcNow
        };

        await _context.Subscriptions.AddAsync(subscription, cancellationToken);
        await _context.OutboxMessages.AddAsync(outboxMessage, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return subscription;
    }

    public async Task<Subscription?> GetSubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        return await _context.Subscriptions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == subscriptionId, cancellationToken);
    }

    public async Task<bool> CancelSubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        var subscription = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.Id == subscriptionId, cancellationToken);

        if (subscription == null)
        {
            return false;
        }

        if (string.Equals(subscription.Status, "cancelled", StringComparison.OrdinalIgnoreCase))
        {
            return true; // idempotent cancel
        }

        subscription.Status = "cancelled";
        subscription.UpdatedAt = DateTime.UtcNow;

        var outboxMessage = new TransactionalOutboxMessage
        {
            Id = Guid.NewGuid(),
            AggregateId = subscription.Id,
            EventType = "SubscriptionCancelled",
            Payload = JsonSerializer.Serialize(subscription),
            CreatedAt = DateTime.UtcNow
        };

        _context.OutboxMessages.Add(outboxMessage);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
