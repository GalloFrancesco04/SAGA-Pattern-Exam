using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using SaaS.Utility.Kafka.Abstractions;

namespace SaaS.Utility.Kafka.Clients;

/// <summary>
/// Implements IProducerClient using Confluent.Kafka
/// </summary>
public class KafkaProducerClient : IProducerClient<string, string>
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaProducerClient> _logger;

    public KafkaProducerClient(
        IProducer<string, string> producer,
        ILogger<KafkaProducerClient> logger)
    {
        _producer = producer;
        _logger = logger;
    }

    /// <summary>
    /// Produces a message to the specified topic
    /// </summary>
    public async Task ProduceAsync(
        string topic,
        string key,
        string value,
        CancellationToken cancellationToken)
    {
        try
        {
            var message = new Confluent.Kafka.Message<string, string>
            {
                Key = key,
                Value = value
            };

            _logger.LogInformation("Producing message to topic {Topic} with key {Key}", topic, key);

            var deliveryReport = await _producer.ProduceAsync(
                topic,
                message,
                cancellationToken);

            if (deliveryReport.Status == PersistenceStatus.Persisted)
            {
                _logger.LogInformation(
                    "Message successfully produced to topic {Topic} at partition {Partition} with offset {Offset}",
                    deliveryReport.Topic,
                    deliveryReport.Partition,
                    deliveryReport.Offset);
            }
            else
            {
                _logger.LogError(
                    "Failed to produce message to topic {Topic}. Status: {Status}",
                    topic,
                    deliveryReport.Status);
            }
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex, "Error producing message to topic {Topic}: {Error}", topic, ex.Error.Reason);
            throw;
        }
    }

    public void Dispose()
    {
        _producer?.Dispose();
    }
}
