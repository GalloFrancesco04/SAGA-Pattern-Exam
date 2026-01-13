using SaaS.Utility.Kafka.Abstractions;

namespace SaaS.Billing.Shared;

public static class BillingTopicNames
{
    public const string SubscriptionCreated = "saas-subscription-created";
    public const string SubscriptionCancelled = "saas-subscription-cancelled";
}

public sealed class BillingTopics : IKafkaTopics
{
    public IEnumerable<string> GetTopics() => new[]
    {
        BillingTopicNames.SubscriptionCreated,
        BillingTopicNames.SubscriptionCancelled
    };
}
