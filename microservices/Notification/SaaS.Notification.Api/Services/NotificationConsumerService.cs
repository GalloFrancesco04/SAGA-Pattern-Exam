using SaaS.Notification.Business.Services;
using SaaS.Utility.Kafka.Abstractions;
using System.Text.Json;

namespace SaaS.Notification.Api.Services;

/// <summary>
/// Background service that consumes commands from the Orchestrator.
/// Consumes: SendEmail command from saas-send-email topic
/// Produces: EmailSentEvent to outbox for publishing to saas-email-sent topic
/// </summary>
public class NotificationConsumerService : BackgroundService
{
    private readonly IConsumerClient<string, string> _consumerClient;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<NotificationConsumerService> _logger;

    // Topics to consume from
    private static readonly string[] ConsumerTopics = new[]
    {
        "saas-send-welcome-email"  // OrchestratorService: SendWelcomeEmail command
    };

    public NotificationConsumerService(
        IConsumerClient<string, string> consumerClient,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<NotificationConsumerService> logger)
    {
        _consumerClient = consumerClient;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("NotificationConsumerService starting. Subscribing to topics: {Topics}",
            string.Join(", ", ConsumerTopics));

        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("NotificationConsumerService consumer loop started");

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
            _logger.LogInformation("NotificationConsumerService cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in NotificationConsumerService");
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
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                // Route based on topic
                switch (topic)
                {
                    case "saas-send-welcome-email":
                        await HandleSendEmailAsync(emailService, messageValue);
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
    /// Handle SendEmail command: send welcome email and emit EmailSentEvent
    /// </summary>
    private async Task HandleSendEmailAsync(IEmailService emailService, string messageValue)
    {
        try
        {
            // Parse the command payload
            using (JsonDocument doc = JsonDocument.Parse(messageValue))
            {
                var root = doc.RootElement;

                var subscriptionId = Guid.Parse(root.GetProperty("SubscriptionId").GetString()!);
                var recipientEmail = root.GetProperty("RecipientEmail").GetString()!;
                var tenantName = root.GetProperty("TenantName").GetString()!;

                _logger.LogInformation("Sending welcome email for SubscriptionId {SubscriptionId} to {RecipientEmail}",
                    subscriptionId, recipientEmail);

                // Send the welcome email
                var emailId = await emailService.SendWelcomeEmailAsync(subscriptionId, recipientEmail, tenantName);

                _logger.LogInformation("Welcome email sent with ID {EmailId} for Subscription {SubscriptionId}",
                    emailId, subscriptionId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling SendEmail command");
            throw;
        }
    }
}
