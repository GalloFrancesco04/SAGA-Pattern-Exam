namespace SaaS.Orchestrator.Shared.DTOs;

/// <summary>
/// Request to start a new subscription SAGA
/// </summary>
public record StartSubscriptionSagaRequest(
    Guid CustomerId,
    string PlanId,
    string TenantName
);

/// <summary>
/// Response when a SAGA is started
/// </summary>
public record StartSagaResponse(
    Guid SagaId,
    string Status,
    string CurrentStep,
    DateTime CreatedAt
);

/// <summary>
/// Response with detailed SAGA status
/// </summary>
public record GetSagaStatusResponse(
    Guid SagaId,
    Guid CustomerId,
    string PlanId,
    string TenantName,
    Guid? SubscriptionId,
    Guid? TenantId,
    Guid? EmailId,
    string Status,
    string CurrentStep,
    string? ErrorMessage,
    bool CompensationNeeded,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? CompletedAt
);
