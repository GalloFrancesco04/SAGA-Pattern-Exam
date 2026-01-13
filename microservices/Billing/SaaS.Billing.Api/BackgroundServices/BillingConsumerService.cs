using SaaS.Billing.Business.Services;
using SaaS.Utility.Kafka.Abstractions;
using System.Text.Json;

namespace SaaS.Billing.Api.BackgroundServices;

/// <summary>
/// Background service that consumes commands from the Orchestrator.
/// Consumes: CreateSubscription command from saas-create-subscription topic
/// Produces: SubscriptionCreatedEvent to outbox for publishing to saas-subscription-created topic
/// </summary>
public class BillingConsumerService : BackgroundService
{
    private readonly IConsumerClient<string, string> _consumerClient;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<BillingConsumerService> _logger;

    // Topics to consume from
    private static readonly string[] ConsumerTopics = new[]
    {
        "saas-create-subscription"  // OrchestratorService: CreateSubscription command
    };

    public BillingConsumerService(
        IConsumerClient<string, string> consumerClient,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<BillingConsumerService> logger)
    {
        _consumerClient = consumerClient;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("BillingConsumerService starting. Subscribing to topics: {Topics}",
            string.Join(", ", ConsumerTopics));

        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("BillingConsumerService consumer loop started");

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
            _logger.LogInformation("BillingConsumerService cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in BillingConsumerService");
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
                var subscriptionService = scope.ServiceProvider.GetRequiredService<ISubscriptionService>();

                // Route based on topic
                switch (topic)
                {
                    case "saas-create-subscription":
                        await HandleCreateSubscriptionAsync(subscriptionService, messageValue);
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
    /// Handle CreateSubscription command: create subscription and emit SubscriptionCreatedEvent
    /// </summary>
    private async Task HandleCreateSubscriptionAsync(ISubscriptionService subscriptionService, string messageValue)
    {
        try
        {
            // Parse the command payload
            using (JsonDocument doc = JsonDocument.Parse(messageValue))
            {
                var root = doc.RootElement;

                var sagaId = Guid.Parse(root.GetProperty("SagaId").GetString()!);
                var customerId = Guid.Parse(root.GetProperty("CustomerId").GetString()!);
                var planId = root.GetProperty("PlanId").GetString()!;

                _logger.LogInformation("Creating subscription for SAGA {SagaId}, Customer {CustomerId}, Plan {PlanId}",
                    sagaId, customerId, planId);

                // Create the subscription
                var subscription = await subscriptionService.CreateSubscriptionAsync(customerId, planId);

                _logger.LogInformation("Subscription created with ID {SubscriptionId} for SAGA {SagaId}",
                    subscription.Id, sagaId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling CreateSubscription command");
            throw;
        }
    }
}
