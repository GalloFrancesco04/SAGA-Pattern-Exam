using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SaaS.Utility.Kafka.Abstractions;

namespace SaaS.Utility.Kafka.Services;

/// <summary>
/// Background service for producing messages to Kafka topics
/// Polls message source and publishes messages
/// </summary>
public abstract class ProducerService<TMessage> : BackgroundService
    where TMessage : class
{
    private readonly IProducerClient<string, string> _producerClient;
    private readonly ILogger<ProducerService<TMessage>> _logger;
    private readonly TimeSpan _pollingInterval;

    protected ProducerService(
        IProducerClient<string, string> producerClient,
        ILogger<ProducerService<TMessage>> logger,
        TimeSpan? pollingInterval = null)
    {
        _producerClient = producerClient;
        _logger = logger;
        _pollingInterval = pollingInterval ?? TimeSpan.FromSeconds(5);
    }

    /// <summary>
    /// Gets the next batch of messages to produce
    /// Must be implemented by derived classes
    /// </summary>
    protected abstract Task<IEnumerable<ProducerMessage<TMessage>>> GetMessagesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Marks a message as successfully produced
    /// Must be implemented by derived classes
    /// </summary>
    protected abstract Task MarkAsProducedAsync(TMessage message, CancellationToken cancellationToken);

    /// <summary>
    /// Executes the producer loop
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ProducerService starting for type {Type}", typeof(TMessage).Name);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var messages = await GetMessagesAsync(stoppingToken);

                foreach (var message in messages)
                {
                    _logger.LogDebug("Producing message to topic {Topic}", message.Topic);

                    try
                    {
                        await _producerClient.ProduceAsync(
                            message.Topic,
                            message.Key,
                            message.Value,
                            stoppingToken);

                        await MarkAsProducedAsync(message.Payload, stoppingToken);

                        _logger.LogInformation("Message produced successfully to topic {Topic}", message.Topic);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error producing message to topic {Topic}", message.Topic);
                        // Message will be retried in next polling cycle
                    }
                }

                await Task.Delay(_pollingInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("ProducerService cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ProducerService encountered an error");
                await Task.Delay(_pollingInterval, stoppingToken);
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("ProducerService stopping");
        _producerClient?.Dispose();
        await base.StopAsync(cancellationToken);
    }
}

/// <summary>
/// Represents a message to be produced to Kafka
/// </summary>
public class ProducerMessage<T>
    where T : class
{
    /// <summary>
    /// Gets or sets the Kafka topic
    /// </summary>
    public required string Topic { get; init; }

    /// <summary>
    /// Gets or sets the message key
    /// </summary>
    public required string Key { get; init; }

    /// <summary>
    /// Gets or sets the serialized message value
    /// </summary>
    public required string Value { get; init; }

    /// <summary>
    /// Gets or sets the original message payload for tracking
    /// </summary>
    public required T Payload { get; init; }
}
