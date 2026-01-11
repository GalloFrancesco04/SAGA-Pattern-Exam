namespace SaaS.Utility.Kafka.Abstractions;

/// <summary>
/// Defines contract for administering Kafka topics
/// </summary>
public interface IAdministatorClient : IDisposable
{
    /// <summary>
    /// Attempts to create a topic if it does not exist
    /// </summary>
    /// <param name="topic">Topic name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task TryCreateTopicsAsync(string topic, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to create multiple topics if they do not exist
    /// </summary>
    /// <param name="topics">Topic names</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task TryCreateTopicsAsync(IEnumerable<string> topics, CancellationToken cancellationToken = default);
}
