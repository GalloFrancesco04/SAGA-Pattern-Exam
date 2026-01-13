using Microsoft.EntityFrameworkCore;
using SaaS.Billing.Repository.Contexts;
using SaaS.Billing.Shared.Entities;
using SaaS.Utility.Kafka.Abstractions;
using SaaS.Utility.Kafka.Services;

namespace SaaS.Billing.Api.BackgroundServices;

public sealed class BillingProducerService : ProducerService<TransactionalOutboxMessage>
{
    private readonly IServiceScopeFactory _scopeFactory;

    public BillingProducerService(
        IProducerClient<string, string> producerClient,
        ILogger<BillingProducerService> logger,
        IServiceScopeFactory scopeFactory) : base(producerClient, logger, TimeSpan.FromSeconds(5))
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task<IEnumerable<ProducerMessage<TransactionalOutboxMessage>>> GetMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();

        var pending = await db.OutboxMessages
            .AsNoTracking()
            .Where(m => !m.IsProduced)
            .OrderBy(m => m.CreatedAt)
            .Take(50)
            .ToListAsync(cancellationToken);

        return pending.Select(m => new ProducerMessage<TransactionalOutboxMessage>
        {
            Topic = m.EventType,
            Key = m.AggregateId.ToString(),
            Value = m.Payload,
            Payload = m
        });
    }

    protected override async Task MarkAsProducedAsync(TransactionalOutboxMessage message, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();

        var entity = await db.OutboxMessages.FirstOrDefaultAsync(x => x.Id == message.Id, cancellationToken);
        if (entity == null)
        {
            return;
        }

        entity.IsProduced = true;
        entity.ProducedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
    }
}
