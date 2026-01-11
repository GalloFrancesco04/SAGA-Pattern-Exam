namespace SaaS.Utility.Kafka.Exceptions;

/// <summary>
/// Base exception for Kafka-related errors
/// </summary>
public class KafkaOperationException : Exception
{
    public KafkaOperationException(string message) : base(message)
    {
    }

    public KafkaOperationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception for message production failures
/// </summary>
public class MessageProductionException : KafkaOperationException
{
    public string? Topic { get; }
    public string? MessageKey { get; }

    public MessageProductionException(string message, string? topic = null, string? messageKey = null)
        : base(message)
    {
        Topic = topic;
        MessageKey = messageKey;
    }

    public MessageProductionException(
        string message,
        Exception innerException,
        string? topic = null,
        string? messageKey = null)
        : base(message, innerException)
    {
        Topic = topic;
        MessageKey = messageKey;
    }
}

/// <summary>
/// Exception for message consumption failures
/// </summary>
public class MessageConsumptionException : KafkaOperationException
{
    public string? Topic { get; }

    public MessageConsumptionException(string message, string? topic = null)
        : base(message)
    {
        Topic = topic;
    }

    public MessageConsumptionException(string message, Exception innerException, string? topic = null)
        : base(message, innerException)
    {
        Topic = topic;
    }
}

/// <summary>
/// Exception for topic administration failures
/// </summary>
public class TopicAdministrationException : KafkaOperationException
{
    public IReadOnlyList<string> Topics { get; }

    public TopicAdministrationException(string message, params string[] topics)
        : base(message)
    {
        Topics = topics.AsReadOnly();
    }

    public TopicAdministrationException(string message, Exception innerException, params string[] topics)
        : base(message, innerException)
    {
        Topics = topics.AsReadOnly();
    }
}
