namespace SaaS.Utility.Kafka.Abstractions;

/// <summary>
/// Defines contract for consuming messages from Kafka
/// </summary>
public interface IConsumerClient<TKey, TValue> : IDisposable
{
    /// <summary>
    /// Consumes messages in a loop from a list of topics
    /// </summary>
    /// <param name="topics">Topics to consume from</param>
    /// <param name="onMessageReceived">Action to execute when a message is received</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ConsumeInLoopAsync(
        IEnumerable<string> topics,
        Func<ConsumedMessage<TKey, TValue>, Task> onMessageReceived,
        CancellationToken cancellationToken);
}

/// <summary>
/// Represents a message consumed from Kafka
/// </summary>
public class ConsumedMessage<TKey, TValue>
{
    public required string Topic { get; init; }
    public required Message<TKey, TValue> Message { get; init; }
}

/// <summary>
/// Represents a Kafka message
/// </summary>
public class Message<TKey, TValue>
{
    public required TKey Key { get; init; }
    public required TValue Value { get; init; }
}
