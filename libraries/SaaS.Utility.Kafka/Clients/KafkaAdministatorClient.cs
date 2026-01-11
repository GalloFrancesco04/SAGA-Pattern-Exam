using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Logging;
using SaaS.Utility.Kafka.Abstractions;

namespace SaaS.Utility.Kafka.Clients;

/// <summary>
/// Implements IAdministatorClient using Confluent.Kafka AdminClient
/// </summary>
public class KafkaAdministatorClient : IAdministatorClient
{
    private readonly IAdminClient _adminClient;
    private readonly ILogger<KafkaAdministatorClient> _logger;

    public KafkaAdministatorClient(
        IAdminClient adminClient,
        ILogger<KafkaAdministatorClient> logger)
    {
        _adminClient = adminClient;
        _logger = logger;
    }

    /// <summary>
    /// Attempts to create a single topic if it does not exist
    /// </summary>
    public async Task TryCreateTopicsAsync(string topic, CancellationToken cancellationToken = default)
    {
        await TryCreateTopicsAsync(new[] { topic }, cancellationToken);
    }

    /// <summary>
    /// Attempts to create multiple topics if they do not exist
    /// </summary>
    public async Task TryCreateTopicsAsync(IEnumerable<string> topics, CancellationToken cancellationToken = default)
    {
        var topicsList = topics.ToList();

        try
        {
            // Get existing topics
            var metadata = _adminClient.GetMetadata(TimeSpan.FromSeconds(10));
            var existingTopics = metadata.Topics.Select(t => t.Topic).ToHashSet();

            // Filter topics that don't exist
            var topicsToCreate = topicsList
                .Where(t => !existingTopics.Contains(t))
                .ToList();

            if (!topicsToCreate.Any())
            {
                _logger.LogInformation("All topics already exist: {Topics}", string.Join(", ", topicsList));
                return;
            }

            // Create topic specifications
            var topicSpecifications = topicsToCreate
                .Select(t => new TopicSpecification
                {
                    Name = t,
                    NumPartitions = 1,
                    ReplicationFactor = 1
                })
                .ToList();

            _logger.LogInformation("Creating topics: {Topics}", string.Join(", ", topicsToCreate));

            await _adminClient.CreateTopicsAsync(topicSpecifications);

            _logger.LogInformation("Topics created successfully: {Topics}", string.Join(", ", topicsToCreate));
        }
        catch (CreateTopicsException ex)
        {
            _logger.LogError(ex, "Error creating topics");
            // Log but don't throw - topics might already exist
        }
        catch (KafkaException ex)
        {
            _logger.LogError(ex, "Kafka error creating topics");
        }
    }

    public void Dispose()
    {
        _adminClient?.Dispose();
    }
}
