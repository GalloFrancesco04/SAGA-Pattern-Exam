using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SaaS.Notification.Repository.Contexts;
using SaaS.Notification.Shared;
using SaaS.Notification.Shared.Entities;
using SaaS.Utility.Kafka.Abstractions;
using SaaS.Utility.Kafka.Services;

namespace SaaS.Notification.Api.Services;

/// <summary>
/// Background service that polls the transactional outbox and publishes messages to Kafka
/// </summary>
public class NotificationProducerService : ProducerService<TransactionalOutboxMessage>
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public NotificationProducerService(
        IProducerClient<string, string> producerClient,
        ILogger<NotificationProducerService> logger,
        IServiceScopeFactory serviceScopeFactory)
        : base(producerClient, logger, TimeSpan.FromSeconds(5))
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    protected override async Task<IEnumerable<ProducerMessage<TransactionalOutboxMessage>>> GetMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

        var pending = await dbContext.OutboxMessages
            .AsNoTracking()
            .Where(m => !m.IsProduced)
            .OrderBy(m => m.CreatedAt)
            .Take(50)
            .ToListAsync(cancellationToken);

        return pending.Select(m => new ProducerMessage<TransactionalOutboxMessage>
        {
            Topic = m.EventType switch
            {
                "EmailSent" => NotificationTopicNames.EmailSent,
                _ => throw new InvalidOperationException($"Unknown event type: {m.EventType}")
            },
            Key = m.AggregateId.ToString(),
            Value = m.Payload,
            Payload = m
        });
    }

    protected override async Task MarkAsProducedAsync(TransactionalOutboxMessage message, CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

        var outboxMessage = await dbContext.OutboxMessages
            .FirstOrDefaultAsync(m => m.Id == message.Id, cancellationToken);

        if (outboxMessage != null)
        {
            outboxMessage.IsProduced = true;
            outboxMessage.ProducedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
