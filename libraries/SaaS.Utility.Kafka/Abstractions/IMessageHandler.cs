namespace SaaS.Utility.Kafka.Abstractions;

/// <summary>
/// Defines contract for handling messages received from Kafka
/// </summary>
public interface IMessageHandler<TKey, TValue>
{
    /// <summary>
    /// Processes a received message
    /// </summary>
    /// <param name="key">Message key</param>
    /// <param name="value">Message value</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task OnMessageReceivedAsync(TKey key, TValue value, CancellationToken cancellationToken = default);
}
