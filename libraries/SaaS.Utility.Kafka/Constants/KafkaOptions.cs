namespace SaaS.Utility.Kafka.Constants;

/// <summary>
/// Kafka configuration options
/// </summary>
public class KafkaOptions
{
    /// <summary>
    /// Gets or sets the bootstrap servers (comma-separated list)
    /// </summary>
    public string BootstrapServers { get; set; } = "localhost:9092";

    /// <summary>
    /// Gets or sets the consumer group ID
    /// </summary>
    public string? GroupId { get; set; }

    /// <summary>
    /// Gets or sets the auto offset reset policy
    /// </summary>
    public string AutoOffsetReset { get; set; } = "earliest";

    /// <summary>
    /// Gets or sets the security protocol
    /// </summary>
    public string? SecurityProtocol { get; set; }

    /// <summary>
    /// Gets or sets the SASL mechanism
    /// </summary>
    public string? SaslMechanism { get; set; }

    /// <summary>
    /// Gets or sets the SASL username
    /// </summary>
    public string? SaslUsername { get; set; }

    /// <summary>
    /// Gets or sets the SASL password
    /// </summary>
    public string? SaslPassword { get; set; }

    /// <summary>
    /// Gets or sets request timeout in milliseconds
    /// </summary>
    public int RequestTimeoutMs { get; set; } = 30000;
}
