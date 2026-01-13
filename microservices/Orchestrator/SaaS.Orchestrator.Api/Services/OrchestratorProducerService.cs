using Microsoft.EntityFrameworkCore;
using SaaS.Orchestrator.Repository.Contexts;
using SaaS.Orchestrator.Shared;
using SaaS.Orchestrator.Shared.Entities;
using SaaS.Utility.Kafka.Abstractions;
using SaaS.Utility.Kafka.Services;

namespace SaaS.Orchestrator.Api.Services;

/// <summary>
/// Background service that polls orchestrator outbox messages and produces them to Kafka
/// </summary>
public class OrchestratorProducerService : ProducerService<TransactionalOutboxMessage>
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public OrchestratorProducerService(
        IProducerClient<string, string> producerClient,
        ILogger<OrchestratorProducerService> logger,
        IServiceScopeFactory serviceScopeFactory)
        : base(producerClient, logger, TimeSpan.FromSeconds(5))
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    protected override async Task<IEnumerable<ProducerMessage<TransactionalOutboxMessage>>> GetMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OrchestratorDbContext>();

        var messages = await context.OutboxMessages
            .Where(m => !m.IsProduced)
            .OrderBy(m => m.CreatedAt)
            .Take(50)
            .ToListAsync(cancellationToken);

        return messages.Select(m => new ProducerMessage<TransactionalOutboxMessage>
        {
            Topic = MapEventTypeToTopic(m.EventType),
            Key = m.AggregateId.ToString(),
            Value = m.Payload,
            Payload = m
        });
    }

    protected override async Task MarkAsProducedAsync(TransactionalOutboxMessage message, CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OrchestratorDbContext>();

        var entity = await context.OutboxMessages.FindAsync(new object[] { message.Id }, cancellationToken);
        if (entity != null)
        {
            entity.IsProduced = true;
            entity.ProducedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    private static string MapEventTypeToTopic(string eventType) => eventType switch
    {
        "CreateSubscription" => OrchestratorTopicNames.CreateSubscription,
        "ProvisionTenant" => OrchestratorTopicNames.ProvisionTenant,
        "SendWelcomeEmail" => OrchestratorTopicNames.SendWelcomeEmail,
        "CancelSubscription" => OrchestratorTopicNames.CancelSubscription,
        "DeprovisionTenant" => OrchestratorTopicNames.DeprovisionTenant,
        _ => throw new InvalidOperationException($"Unknown event type: {eventType}")
    };
}
