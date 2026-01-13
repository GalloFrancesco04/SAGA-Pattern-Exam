using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SaaS.Notification.Repository.Contexts;
using SaaS.Notification.Shared;
using SaaS.Notification.Shared.Entities;
using SaaS.Utility.Kafka.Abstractions.Clients;
using SaaS.Utility.Kafka.Services;

namespace SaaS.Notification.Api.Services;

/// <summary>
/// Background service that polls the transactional outbox and publishes messages to Kafka
/// </summary>
public class NotificationProducerService : ProducerService<TransactionalOutboxMessage>
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public NotificationProducerService(
        IServiceScopeFactory serviceScopeFactory,
        IProducerClient producerClient)
        : base(producerClient)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    protected override string GetTopic(TransactionalOutboxMessage message)
    {
        return message.EventType switch
        {
            "EmailSent" => NotificationTopicNames.EmailSent,
            _ => throw new InvalidOperationException($"Unknown event type: {message.EventType}")
        };
    }

    protected override string GetKey(TransactionalOutboxMessage message)
    {
        return message.AggregateId.ToString();
    }

    protected override string GetValue(TransactionalOutboxMessage message)
    {
        return message.Payload;
    }

    protected override async Task<IEnumerable<TransactionalOutboxMessage>> GetMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

        return await dbContext.OutboxMessages
            .Where(m => !m.IsProduced)
            .OrderBy(m => m.CreatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);
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
