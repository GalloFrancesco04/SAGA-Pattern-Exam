using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using SaaS.Utility.Kafka.Abstractions;

namespace SaaS.Utility.Kafka.Clients;

/// <summary>
/// Implements IConsumerClient using Confluent.Kafka
/// </summary>
public class KafkaConsumerClient : IConsumerClient<string, string>
{
    private readonly IConsumer<string, string> _consumer;
    private readonly ILogger<KafkaConsumerClient> _logger;

    public KafkaConsumerClient(
        IConsumer<string, string> consumer,
        ILogger<KafkaConsumerClient> logger)
    {
        _consumer = consumer;
        _logger = logger;
    }

    /// <summary>
    /// Consumes messages in a loop from specified topics
    /// </summary>
    public async Task ConsumeInLoopAsync(
        IEnumerable<string> topics,
        Func<ConsumedMessage<string, string>, Task> onMessageReceived,
        CancellationToken cancellationToken)
    {
        var topicList = topics.ToList();
        _logger.LogInformation("Starting consumption from topics: {Topics}", string.Join(", ", topicList));

        // Retry logic for topic subscription (topics might not exist yet)
        int retryCount = 0;
        const int maxRetries = 10;
        const int initialDelayMs = 2000;

        while (retryCount < maxRetries)
        {
            try
            {
                _consumer.Subscribe(topicList);
                _logger.LogInformation("Successfully subscribed to topics");
                break;
            }
            catch (Exception ex)
            {
                retryCount++;
                int delayMs = initialDelayMs * (int)Math.Pow(2, retryCount - 1); // Exponential backoff
                _logger.LogWarning("Failed to subscribe to topics (attempt {Attempt}/{Max}). Retrying in {DelayMs}ms. Error: {Error}",
                    retryCount, maxRetries, delayMs, ex.Message);

                if (retryCount >= maxRetries)
                {
                    _logger.LogError("Failed to subscribe to topics after {MaxRetries} attempts", maxRetries);
                    throw;
                }

                await Task.Delay(delayMs, cancellationToken);
            }
        }

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = _consumer.Consume(cancellationToken);

                    if (consumeResult == null)
                        continue;

                    if (consumeResult.IsPartitionEOF)
                    {
                        _logger.LogDebug("Reached end of partition {Partition} on topic {Topic}", consumeResult.Partition, consumeResult.Topic);
                        continue;
                    }

                    _logger.LogInformation("Received message from topic {Topic}: Key={Key}", consumeResult.Topic, consumeResult.Message.Key);

                    var consumedMessage = new ConsumedMessage<string, string>
                    {
                        Topic = consumeResult.Topic,
                        Message = new Abstractions.Message<string, string>
                        {
                            Key = consumeResult.Message.Key,
                            Value = consumeResult.Message.Value
                        }
                    };

                    await onMessageReceived(consumedMessage);

                    _consumer.Commit(consumeResult);
                }
                catch (Confluent.Kafka.TopicAuthorizationException ex) when (!cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("Topic authorization error: {Error}. Retrying in 5 seconds...", ex.Message);
                    await Task.Delay(5000, cancellationToken);
                }
                catch (Confluent.Kafka.ConsumeException ex) when (ex.Message.Contains("Unknown topic") && !cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("Topic not available yet: {Error}. Retrying in 5 seconds...", ex.Message);
                    await Task.Delay(5000, cancellationToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Consumer cancelled");
        }
        finally
        {
            _consumer.Close();
        }
    }

    public void Dispose()
    {
        _consumer?.Dispose();
    }
}
