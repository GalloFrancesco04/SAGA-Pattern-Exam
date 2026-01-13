namespace SaaS.Orchestrator.Shared;

/// <summary>
/// Kafka topic names for Orchestrator commands
/// </summary>
public static class OrchestratorTopicNames
{
    public const string CreateSubscription = "saas-create-subscription";
    public const string ProvisionTenant = "saas-provision-tenant";
    public const string SendWelcomeEmail = "saas-send-welcome-email";
    public const string CancelSubscription = "saas-cancel-subscription";
    public const string DeprovisionTenant = "saas-deprovision-tenant";
}
