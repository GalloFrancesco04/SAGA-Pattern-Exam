namespace SaaS.Utility.Kafka.Abstractions;

/// <summary>
/// Defines Kafka topics managed by a service
/// </summary>
public interface IKafkaTopics
{
    /// <summary>
    /// Returns the list of all topics managed by this service
    /// </summary>
    IEnumerable<string> GetTopics();
}
