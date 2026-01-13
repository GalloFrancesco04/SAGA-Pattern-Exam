using SaaS.Orchestrator.Business.Services;
using SaaS.Utility.Kafka.Abstractions;
using System.Text.Json;

namespace SaaS.Orchestrator.Api.Services;

/// <summary>
/// Background service that consumes SAGA-related events from other microservices.
/// Reads: SubscriptionCreatedEvent, TenantProvisionedEvent, TenantProvisioningFailedEvent, EmailSentEvent
/// Drives the SAGA state machine forward by calling appropriate SagaService handlers.
/// </summary>
public class OrchestratorConsumerService : BackgroundService
{
    private readonly IConsumerClient<string, string> _consumerClient;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<OrchestratorConsumerService> _logger;

    // Topics to consume from
    private static readonly string[] ConsumerTopics = new[]
    {
        "saas-subscription-created",      // BillingService: SubscriptionCreatedEvent (PIVOT)
        "saas-tenant-provisioned",        // ProvisioningService: TenantProvisionedEvent
        "saas-tenant-provisioning-failed", // ProvisioningService: TenantProvisioningFailedEvent
        "saas-email-sent"                 // NotificationService: EmailSentEvent
    };

    public OrchestratorConsumerService(
        IConsumerClient<string, string> consumerClient,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<OrchestratorConsumerService> logger)
    {
        _consumerClient = consumerClient;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("OrchestratorConsumerService starting. Subscribing to topics: {Topics}",
            string.Join(", ", ConsumerTopics));

        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("OrchestratorConsumerService consumer loop started");

            // Use the ConsumeInLoopAsync pattern from IConsumerClient
            await _consumerClient.ConsumeInLoopAsync(
                ConsumerTopics,
                ProcessMessageAsync,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("OrchestratorConsumerService cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in OrchestratorConsumerService");
            throw;
        }
        finally
        {
            _consumerClient?.Dispose();
        }
    }

    /// <summary>
    /// Process a single message and route to appropriate handler based on topic and event type.
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
                var sagaService = scope.ServiceProvider.GetRequiredService<ISagaService>();

                // Route based on topic
                switch (topic)
                {
                    case "saas-subscription-created":
                        await HandleSubscriptionCreatedAsync(sagaService, messageValue);
                        break;

                    case "saas-tenant-provisioned":
                        await HandleTenantProvisionedAsync(sagaService, messageValue);
                        break;

                    case "saas-tenant-provisioning-failed":
                        await HandleTenantProvisioningFailedAsync(sagaService, messageValue);
                        break;

                    case "saas-email-sent":
                        await HandleEmailSentAsync(sagaService, messageValue);
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
            _logger.LogError(ex, "Error processing message. Topic: {Topic}, Key: {Key}",
                message.Topic, message.Message.Key);
            // Continue processing other messages, don't rethrow
        }
    }

    /// <summary>
    /// Handle SubscriptionCreatedEvent from BillingService - This is the PIVOT point.
    /// </summary>
    private async Task HandleSubscriptionCreatedAsync(
        ISagaService sagaService,
        string messageValue)
    {
        try
        {
            // Parse event: { "sagaId": "...", "subscriptionId": "..." }
            using (var jsonDoc = JsonDocument.Parse(messageValue))
            {
                var root = jsonDoc.RootElement;

                if (!Guid.TryParse(root.GetProperty("sagaId").GetString(), out var sagaId))
                    throw new FormatException("Invalid sagaId format");

                if (!Guid.TryParse(root.GetProperty("subscriptionId").GetString(), out var subscriptionId))
                    throw new FormatException("Invalid subscriptionId format");

                _logger.LogInformation("SubscriptionCreated event - PIVOT. SagaId: {SagaId}, SubscriptionId: {SubscriptionId}",
                    sagaId, subscriptionId);

                // Mark PIVOT: subscription created, now provision tenant required
                await sagaService.HandleSubscriptionCreatedAsync(sagaId, subscriptionId);

                _logger.LogInformation("SAGA {SagaId} progressed to Provisioning state", sagaId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling SubscriptionCreatedEvent: {MessageValue}", messageValue);
            throw;
        }
    }

    /// <summary>
    /// Handle TenantProvisionedEvent from ProvisioningService.
    /// </summary>
    private async Task HandleTenantProvisionedAsync(
        ISagaService sagaService,
        string messageValue)
    {
        try
        {
            // Parse event: { "sagaId": "...", "tenantId": "..." }
            using (var jsonDoc = JsonDocument.Parse(messageValue))
            {
                var root = jsonDoc.RootElement;

                if (!Guid.TryParse(root.GetProperty("sagaId").GetString(), out var sagaId))
                    throw new FormatException("Invalid sagaId format");

                if (!Guid.TryParse(root.GetProperty("tenantId").GetString(), out var tenantId))
                    throw new FormatException("Invalid tenantId format");

                _logger.LogInformation("TenantProvisioned event. SagaId: {SagaId}, TenantId: {TenantId}",
                    sagaId, tenantId);

                // Tenant created, now send welcome email
                await sagaService.HandleTenantProvisionedAsync(sagaId, tenantId);

                _logger.LogInformation("SAGA {SagaId} progressed to Notifying state", sagaId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling TenantProvisionedEvent: {MessageValue}", messageValue);
            throw;
        }
    }

    /// <summary>
    /// Handle TenantProvisioningFailedEvent from ProvisioningService - triggers compensation.
    /// </summary>
    private async Task HandleTenantProvisioningFailedAsync(
        ISagaService sagaService,
        string messageValue)
    {
        try
        {
            // Parse event: { "sagaId": "...", "errorMessage": "..." }
            using (var jsonDoc = JsonDocument.Parse(messageValue))
            {
                var root = jsonDoc.RootElement;

                if (!Guid.TryParse(root.GetProperty("sagaId").GetString(), out var sagaId))
                    throw new FormatException("Invalid sagaId format");

                var errorMessage = root.GetProperty("errorMessage").GetString() ?? "Unknown error";

                _logger.LogWarning("TenantProvisioningFailed event. SagaId: {SagaId}, Error: {ErrorMessage}",
                    sagaId, errorMessage);

                // Provisioning failed, trigger compensation
                await sagaService.HandleTenantProvisioningFailedAsync(sagaId, errorMessage);

                _logger.LogInformation("SAGA {SagaId} triggered compensation", sagaId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling TenantProvisioningFailedEvent: {MessageValue}", messageValue);
            throw;
        }
    }

    /// <summary>
    /// Handle EmailSentEvent from NotificationService - marks SAGA as completed.
    /// </summary>
    private async Task HandleEmailSentAsync(
        ISagaService sagaService,
        string messageValue)
    {
        try
        {
            // Parse event: { "sagaId": "...", "emailId": "..." }
            using (var jsonDoc = JsonDocument.Parse(messageValue))
            {
                var root = jsonDoc.RootElement;

                if (!Guid.TryParse(root.GetProperty("sagaId").GetString(), out var sagaId))
                    throw new FormatException("Invalid sagaId format");

                if (!Guid.TryParse(root.GetProperty("emailId").GetString(), out var emailId))
                    throw new FormatException("Invalid emailId format");

                _logger.LogInformation("EmailSent event. SagaId: {SagaId}, EmailId: {EmailId}",
                    sagaId, emailId);

                // Email sent, SAGA completed successfully
                await sagaService.HandleEmailSentAsync(sagaId, emailId);

                _logger.LogInformation("SAGA {SagaId} completed successfully ✅", sagaId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling EmailSentEvent: {MessageValue}", messageValue);
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("OrchestratorConsumerService stopping");
        await base.StopAsync(cancellationToken);
    }
}
