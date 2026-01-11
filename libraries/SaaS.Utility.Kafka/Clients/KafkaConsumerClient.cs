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

        _consumer.Subscribe(topicList);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
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
