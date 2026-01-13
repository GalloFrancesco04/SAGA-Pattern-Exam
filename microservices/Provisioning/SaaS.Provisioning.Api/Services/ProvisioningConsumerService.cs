using SaaS.Provisioning.Business.Services;
using SaaS.Utility.Kafka.Abstractions;
using System.Text.Json;

namespace SaaS.Provisioning.Api.Services;

/// <summary>
/// Background service that consumes commands from the Orchestrator.
/// Consumes: CreateTenant command from saas-provision-tenant topic
/// Produces: TenantProvisionedEvent or TenantProvisioningFailedEvent to outbox
/// </summary>
public class ProvisioningConsumerService : BackgroundService
{
    private readonly IConsumerClient<string, string> _consumerClient;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<ProvisioningConsumerService> _logger;

    // Topics to consume from
    private static readonly string[] ConsumerTopics = new[]
    {
        "saas-provision-tenant"  // OrchestratorService: CreateTenant command
    };

    public ProvisioningConsumerService(
        IConsumerClient<string, string> consumerClient,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<ProvisioningConsumerService> logger)
    {
        _consumerClient = consumerClient;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("ProvisioningConsumerService starting. Subscribing to topics: {Topics}",
            string.Join(", ", ConsumerTopics));

        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("ProvisioningConsumerService consumer loop started");

            // Wait a bit for Kafka to be ready
            await Task.Delay(2000, cancellationToken);

            // Use the ConsumeInLoopAsync pattern from IConsumerClient
            await _consumerClient.ConsumeInLoopAsync(
                ConsumerTopics,
                ProcessMessageAsync,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("ProvisioningConsumerService cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in ProvisioningConsumerService");
            throw;
        }
        finally
        {
            _consumerClient?.Dispose();
        }
    }

    /// <summary>
    /// Process a single message and route to appropriate handler based on topic.
    /// </summary>
    private async Task ProcessMessageAsync(ConsumedMessage<string, string> message)
    {
        try
        {
            var topic = message.Topic;
            var messageValue = message.Message.Value;

            _logger.LogInformation("Processing message from topic: {Topic}, Key: {Key}", topic, message.Message.Key);

            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var tenantService = scope.ServiceProvider.GetRequiredService<ITenantService>();

                // Route based on topic
                switch (topic)
                {
                    case "saas-provision-tenant":
                        await HandleProvisionTenantAsync(tenantService, messageValue);
                        break;

                    default:
                        _logger.LogWarning("Unknown topic received: {Topic}", topic);
                        break;
                }
            }

            _logger.LogInformation("Successfully processed message from topic: {Topic}", topic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message from topic: {Topic}", message.Topic);
            // Continue processing instead of throwing to avoid consumer crash
        }
    }

    /// <summary>
    /// Handle ProvisionTenant command: create tenant and emit TenantProvisionedEvent
    /// </summary>
    private async Task HandleProvisionTenantAsync(ITenantService tenantService, string messageValue)
    {
        try
        {
            // Parse the command payload
            using (JsonDocument doc = JsonDocument.Parse(messageValue))
            {
                var root = doc.RootElement;

                var subscriptionId = Guid.Parse(root.GetProperty("SubscriptionId").GetString()!);
                var tenantName = root.GetProperty("TenantName").GetString()!;

                _logger.LogInformation("Provisioning tenant for SubscriptionId {SubscriptionId}, TenantName {TenantName}",
                    subscriptionId, tenantName);

                // Create the tenant
                var tenant = await tenantService.ProvisionTenantAsync(subscriptionId, tenantName);

                _logger.LogInformation("Tenant provisioned with ID {TenantId} for Subscription {SubscriptionId}",
                    tenant.Id, subscriptionId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling ProvisionTenant command");
            throw;
        }
    }
}
