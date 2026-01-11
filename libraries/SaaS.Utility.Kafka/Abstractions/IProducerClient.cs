namespace SaaS.Utility.Kafka.Abstractions;

/// <summary>
/// Defines contract for producing messages to Kafka
/// </summary>
public interface IProducerClient<TKey, TValue> : IDisposable
{
    /// <summary>
    /// Produces a message to a topic
    /// </summary>
    /// <param name="topic">Destination topic</param>
    /// <param name="key">Message key</param>
    /// <param name="value">Message value</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ProduceAsync(string topic, TKey key, TValue value, CancellationToken cancellationToken);
}
