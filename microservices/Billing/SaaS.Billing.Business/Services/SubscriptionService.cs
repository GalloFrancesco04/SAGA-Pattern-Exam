using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SaaS.Billing.Repository.Contexts;
using SaaS.Billing.Shared.Entities;
using SaaS.Billing.Shared.Messages;
using SaaS.Billing.Shared;

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

        var createdEvent = new SubscriptionCreatedEvent
        {
            SubscriptionId = subscription.Id,
            CustomerId = subscription.CustomerId,
            PlanId = subscription.PlanId,
            Status = subscription.Status,
            CreatedAt = subscription.CreatedAt
        };

        var outboxMessage = new TransactionalOutboxMessage
        {
            Id = Guid.NewGuid(),
            AggregateId = subscription.Id,
            EventType = BillingTopicNames.SubscriptionCreated,
            Payload = JsonSerializer.Serialize(createdEvent),
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

        var cancelledEvent = new SubscriptionCancelledEvent
        {
            SubscriptionId = subscription.Id,
            CustomerId = subscription.CustomerId,
            PlanId = subscription.PlanId,
            Status = subscription.Status,
            CancelledAt = subscription.UpdatedAt
        };

        var outboxMessage = new TransactionalOutboxMessage
        {
            Id = Guid.NewGuid(),
            AggregateId = subscription.Id,
            EventType = BillingTopicNames.SubscriptionCancelled,
            Payload = JsonSerializer.Serialize(cancelledEvent),
            CreatedAt = DateTime.UtcNow
        };

        _context.OutboxMessages.Add(outboxMessage);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<List<Subscription>> GetAllSubscriptionsAsync(string? status = null, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        var query = _context.Subscriptions.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(s => s.Status == status);
        }

        return await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }
}

