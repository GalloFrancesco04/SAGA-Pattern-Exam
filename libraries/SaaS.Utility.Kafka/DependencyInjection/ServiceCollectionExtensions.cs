using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SaaS.Utility.Kafka.Abstractions;
using SaaS.Utility.Kafka.Clients;
using SaaS.Utility.Kafka.Constants;

namespace SaaS.Utility.Kafka.DependencyInjection;

/// <summary>
/// Extension methods for adding Kafka services to dependency injection container
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Kafka Confluent client factory to the service collection
    /// </summary>
    public static IServiceCollection AddKafkaClients(
        this IServiceCollection services,
        Action<KafkaOptions> configureOptions)
    {
        // Configure KafkaOptions
        services.Configure(configureOptions);

        // Register Confluent client factories
        services.AddSingleton<IConsumer<string, string>>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<KafkaOptions>>().Value;
            var config = new ConsumerConfig
            {
                BootstrapServers = options.BootstrapServers,
                GroupId = options.GroupId ?? Guid.NewGuid().ToString(),
                AutoOffsetReset = (AutoOffsetReset?)Enum.Parse(typeof(AutoOffsetReset), options.AutoOffsetReset, true) ?? AutoOffsetReset.Earliest,
                EnableAutoCommit = false
            };

            ApplySecurityConfig(config, options);

            return new ConsumerBuilder<string, string>(config)
                .SetKeyDeserializer(Deserializers.Utf8)
                .SetValueDeserializer(Deserializers.Utf8)
                .Build();
        });

        services.AddSingleton<IProducer<string, string>>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<KafkaOptions>>().Value;
            var config = new ProducerConfig
            {
                BootstrapServers = options.BootstrapServers,
                Acks = Acks.All,
                CompressionType = CompressionType.Snappy,
                RequestTimeoutMs = options.RequestTimeoutMs
            };

            ApplySecurityConfig(config, options);

            return new ProducerBuilder<string, string>(config)
                .SetKeySerializer(Serializers.Utf8)
                .SetValueSerializer(Serializers.Utf8)
                .Build();
        });

        services.AddSingleton<IAdminClient>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<KafkaOptions>>().Value;
            var config = new AdminClientConfig
            {
                BootstrapServers = options.BootstrapServers
            };

            ApplySecurityConfig(config, options);

            return new AdminClientBuilder(config).Build();
        });

        // Register client wrappers
        services.AddSingleton<IConsumerClient<string, string>, KafkaConsumerClient>();
        services.AddSingleton<IProducerClient<string, string>, KafkaProducerClient>();
        services.AddSingleton<IAdministatorClient, KafkaAdministatorClient>();

        return services;
    }

    /// <summary>
    /// Applies security configuration (SASL, SSL) to Kafka client config
    /// </summary>
    private static void ApplySecurityConfig(ClientConfig config, KafkaOptions options)
    {
        if (string.IsNullOrEmpty(options.SecurityProtocol))
            return;

        config.SecurityProtocol = (SecurityProtocol?)Enum.Parse(typeof(SecurityProtocol), options.SecurityProtocol, true)
            ?? SecurityProtocol.SaslSsl;

        if (!string.IsNullOrEmpty(options.SaslMechanism))
        {
            config.SaslMechanism = (SaslMechanism?)Enum.Parse(typeof(SaslMechanism), options.SaslMechanism, true)
                ?? SaslMechanism.Plain;
        }

        if (!string.IsNullOrEmpty(options.SaslUsername))
            config.SaslUsername = options.SaslUsername;

        if (!string.IsNullOrEmpty(options.SaslPassword))
            config.SaslPassword = options.SaslPassword;
    }
}
