using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SaaS.Utility.Kafka.Abstractions;

namespace SaaS.Utility.Kafka.Services;

/// <summary>
/// Background service for consuming messages from Kafka topics
/// </summary>
public class ConsumerService<TInput> : BackgroundService
    where TInput : class
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConsumerClient<string, string> _consumerClient;
    private readonly IKafkaTopics _kafkaTopics;
    private readonly IMessageHandlerFactory<string, string> _messageHandlerFactory;
    private readonly ILogger<ConsumerService<TInput>> _logger;

    public ConsumerService(
        IServiceProvider serviceProvider,
        IConsumerClient<string, string> consumerClient,
        IKafkaTopics kafkaTopics,
        IMessageHandlerFactory<string, string> messageHandlerFactory,
        ILogger<ConsumerService<TInput>> logger)
    {
        _serviceProvider = serviceProvider;
        _consumerClient = consumerClient;
        _kafkaTopics = kafkaTopics;
        _messageHandlerFactory = messageHandlerFactory;
        _logger = logger;
    }

    /// <summary>
    /// Executes the consumer loop
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ConsumerService starting for type {Type}", typeof(TInput).Name);

        var topics = _kafkaTopics.GetTopics().ToList();

        if (!topics.Any())
        {
            _logger.LogWarning("No topics configured for consumer");
            return;
        }

        try
        {
            await _consumerClient.ConsumeInLoopAsync(
                topics,
                async (consumedMessage) =>
                {
                    _logger.LogDebug("Processing message from topic {Topic}", consumedMessage.Topic);

                    try
                    {
                        var messageHandler = _messageHandlerFactory.Create(consumedMessage.Topic, _serviceProvider);
                        await messageHandler.OnMessageReceivedAsync(
                            consumedMessage.Message.Key,
                            consumedMessage.Message.Value,
                            stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing message from topic {Topic}", consumedMessage.Topic);
                        // Consider implementing dead letter queue or retry logic here
                    }
                },
                stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("ConsumerService cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ConsumerService encountered an error");
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("ConsumerService stopping");
        _consumerClient?.Dispose();
        await base.StopAsync(cancellationToken);
    }
}
