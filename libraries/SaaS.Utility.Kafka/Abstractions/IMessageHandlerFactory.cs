namespace SaaS.Utility.Kafka.Abstractions;

/// <summary>
/// Factory for creating message handler instances based on topic
/// </summary>
public interface IMessageHandlerFactory<TKey, TValue>
{
    /// <summary>
    /// Creates an appropriate message handler for the specified topic
    /// </summary>
    /// <param name="topic">Topic name</param>
    /// <param name="serviceProvider">Service provider to resolve dependencies</param>
    /// <returns>IMessageHandler instance for the topic</returns>
    IMessageHandler<TKey, TValue> Create(string topic, IServiceProvider serviceProvider);
}
