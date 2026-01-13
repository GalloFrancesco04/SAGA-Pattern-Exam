using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SaaS.Provisioning.Repository.Contexts;
using SaaS.Provisioning.Shared;
using SaaS.Provisioning.Shared.Entities;
using SaaS.Utility.Kafka.Abstractions;
using SaaS.Utility.Kafka.Services;

namespace SaaS.Provisioning.Api.Services;

/// <summary>
/// Background service that polls the Provisioning outbox and publishes events to Kafka
/// Implements Transactional Outbox pattern for reliable async messaging
/// </summary>
public class ProvisioningProducerService : ProducerService<TransactionalOutboxMessage>
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<ProvisioningProducerService> _logger;

    public ProvisioningProducerService(
        IServiceScopeFactory serviceScopeFactory,
        IProducerClient<string, string> producerClient,
        ILogger<ProvisioningProducerService> logger)
        : base(producerClient, logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves unpublished outbox messages from the Provisioning database
    /// </summary>
    protected override async Task<IEnumerable<ProducerMessage<TransactionalOutboxMessage>>> GetMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ProvisioningDbContext>();

        var outboxMessages = await dbContext.OutboxMessages
            .Where(om => !om.IsProduced)
            .OrderBy(om => om.CreatedAt)
            .Take(100) // Process in batches
            .ToListAsync(cancellationToken);

        return outboxMessages
            .Select(om => new ProducerMessage<TransactionalOutboxMessage>
            {
                Topic = om.EventType,
                Key = om.AggregateId.ToString(),
                Value = om.Payload,
                Payload = om
            })
            .ToList();
    }

    /// <summary>
    /// Marks a single outbox message as produced after successful Kafka publication
    /// </summary>
    protected override async Task MarkAsProducedAsync(TransactionalOutboxMessage message, CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ProvisioningDbContext>();

        var outboxMessage = await dbContext.OutboxMessages
            .FirstOrDefaultAsync(om => om.Id == message.Id, cancellationToken);

        if (outboxMessage != null)
        {
            outboxMessage.IsProduced = true;
            outboxMessage.ProducedAt = DateTime.UtcNow;

            dbContext.OutboxMessages.Update(outboxMessage);
            await dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogDebug(
                "Marked outbox message {MessageId} (EventType: {EventType}) as produced",
                message.Id,
                message.EventType);
        }
    }
}