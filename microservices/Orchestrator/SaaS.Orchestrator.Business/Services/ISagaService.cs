using SaaS.Orchestrator.Shared.Entities;

namespace SaaS.Orchestrator.Business.Services;

/// <summary>
/// Service interface for managing SAGA orchestration lifecycle
/// </summary>
public interface ISagaService
{
    /// <summary>
    /// Starts a new subscription SAGA
    /// </summary>
    Task<Guid> StartSubscriptionSagaAsync(Guid customerId, string planId, string tenantName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the current state of a SAGA instance
    /// </summary>
    Task<SagaInstance?> GetSagaInstanceAsync(Guid sagaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Handles subscription created event (PIVOT transaction)
    /// </summary>
    Task HandleSubscriptionCreatedAsync(Guid sagaId, Guid subscriptionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Handles tenant provisioned event
    /// </summary>
    Task HandleTenantProvisionedAsync(Guid sagaId, Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Handles tenant provisioning failed event
    /// </summary>
    Task HandleTenantProvisioningFailedAsync(Guid sagaId, string errorMessage, CancellationToken cancellationToken = default);

    /// <summary>
    /// Handles email sent event
    /// </summary>
    Task HandleEmailSentAsync(Guid sagaId, Guid emailId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Triggers compensation flow for a failed SAGA
    /// </summary>
    Task CompensateSagaAsync(Guid sagaId, CancellationToken cancellationToken = default);
}
