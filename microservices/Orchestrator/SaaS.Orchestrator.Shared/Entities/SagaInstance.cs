namespace SaaS.Orchestrator.Shared.Entities;

/// <summary>
/// Represents a SAGA instance with its current state and progress
/// </summary>
public class SagaInstance
{
    public Guid Id { get; set; }

    // Input data
    public Guid CustomerId { get; set; }
    public string PlanId { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;

    // Aggregate IDs collected during flow
    public Guid? SubscriptionId { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? EmailId { get; set; }

    // State machine
    public string Status { get; set; } = "Pending"; // Pending, Provisioning, Notifying, Completed, Compensating, Compensated, Failed
    public string CurrentStep { get; set; } = "InitiateBilling"; // InitiateBilling, ProvisionTenant, SendWelcomeEmail, Finished

    // Error tracking
    public string? ErrorMessage { get; set; }
    public bool CompensationNeeded { get; set; } = false;

    // Timestamps
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

