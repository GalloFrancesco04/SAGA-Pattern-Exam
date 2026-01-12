namespace SaaS.Orchestrator.Shared.Entities;

public class SagaInstance
{
    public Guid Id { get; set; }
    public Guid SubscriptionId { get; set; }
    public string Status { get; set; } = "pending"; // pending, in-progress, completed, compensating, failed
    public int CurrentStep { get; set; } = 0;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
