using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SaaS.Orchestrator.ClientHttp.Clients;
using SaaS.Orchestrator.Repository.Contexts;
using SaaS.Orchestrator.Shared.Entities;
using System.Text.Json;

namespace SaaS.Orchestrator.Business.Services;

/// <summary>
/// Implementation of SAGA orchestration service with state machine
/// </summary>
public class SagaService : ISagaService
{
    private readonly OrchestratorDbContext _context;
    private readonly ILogger<SagaService> _logger;
    private readonly IBillingClient _billingClient;
    private readonly IProvisioningClient _provisioningClient;

    public SagaService(
        OrchestratorDbContext context,
        ILogger<SagaService> logger,
        IBillingClient billingClient,
        IProvisioningClient provisioningClient)
    {
        _context = context;
        _logger = logger;
        _billingClient = billingClient;
        _provisioningClient = provisioningClient;
    }

    public async Task<Guid> StartSubscriptionSagaAsync(Guid customerId, string planId, string tenantName, CancellationToken cancellationToken = default)
    {
        var sagaId = Guid.NewGuid();

        var sagaInstance = new SagaInstance
        {
            Id = sagaId,
            CustomerId = customerId,
            PlanId = planId,
            TenantName = tenantName,
            Status = "Pending",
            CurrentStep = "InitiateBilling",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.SagaInstances.Add(sagaInstance);

        // Write command to outbox: CreateSubscription
        var outboxMessage = new TransactionalOutboxMessage
        {
            Id = Guid.NewGuid(),
            AggregateId = sagaId,
            EventType = "CreateSubscription",
            Payload = JsonSerializer.Serialize(new
            {
                SagaId = sagaId,
                CustomerId = customerId,
                PlanId = planId
            }),
            CreatedAt = DateTime.UtcNow,
            IsProduced = false
        };

        _context.OutboxMessages.Add(outboxMessage);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Started SAGA {SagaId} for customer {CustomerId}", sagaId, customerId);
        return sagaId;
    }

    public async Task<SagaInstance?> GetSagaInstanceAsync(Guid sagaId, CancellationToken cancellationToken = default)
    {
        return await _context.SagaInstances
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == sagaId, cancellationToken);
    }

    public async Task<SagaInstance?> GetSagaBySubscriptionIdAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        return await _context.SagaInstances
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SubscriptionId == subscriptionId, cancellationToken);
    }

    public async Task<SagaInstance?> GetSagaByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        // Get the most recent Pending saga for this customer
        return await _context.SagaInstances
            .AsNoTracking()
            .Where(s => s.CustomerId == customerId && s.Status == "Pending")
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task HandleSubscriptionCreatedAsync(Guid sagaId, Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        var saga = await _context.SagaInstances.FindAsync(new object[] { sagaId }, cancellationToken);
        if (saga == null)
        {
            _logger.LogWarning("SAGA {SagaId} not found", sagaId);
            return;
        }

        // HTTP synchronous verification: Check subscription status
        var subscriptionStatus = await _billingClient.GetSubscriptionStatusAsync(subscriptionId, cancellationToken);
        if (subscriptionStatus != null)
        {
            _logger.LogInformation("HTTP verification: Subscription {SubscriptionId} status is {Status}, IsActive={IsActive}",
                subscriptionId, subscriptionStatus.Status, subscriptionStatus.IsActive);
        }

        // PIVOT reached - update saga state
        saga.SubscriptionId = subscriptionId;
        saga.Status = "Provisioning";
        saga.CurrentStep = "ProvisionTenant";
        saga.UpdatedAt = DateTime.UtcNow;

        // Write command to outbox: ProvisionTenant
        var outboxMessage = new TransactionalOutboxMessage
        {
            Id = Guid.NewGuid(),
            AggregateId = sagaId,
            EventType = "ProvisionTenant",
            Payload = JsonSerializer.Serialize(new
            {
                SagaId = sagaId,
                SubscriptionId = subscriptionId,
                TenantName = saga.TenantName
            }),
            CreatedAt = DateTime.UtcNow,
            IsProduced = false
        };

        _context.OutboxMessages.Add(outboxMessage);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("SAGA {SagaId} reached PIVOT - subscription {SubscriptionId} created", sagaId, subscriptionId);
    }

    public async Task HandleTenantProvisionedAsync(Guid sagaId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var saga = await _context.SagaInstances.FindAsync(new object[] { sagaId }, cancellationToken);
        if (saga == null)
        {
            _logger.LogWarning("SAGA {SagaId} not found", sagaId);
            return;
        }

        // HTTP synchronous verification: Check tenant status
        var tenantStatus = await _provisioningClient.GetTenantStatusAsync(tenantId, cancellationToken);
        if (tenantStatus != null)
        {
            _logger.LogInformation("HTTP verification: Tenant {TenantId} status is {Status}, ReadyForUse={ReadyForUse}",
                tenantId, tenantStatus.Status, tenantStatus.ReadyForUse);
        }

        saga.TenantId = tenantId;
        saga.Status = "Notifying";
        saga.CurrentStep = "SendWelcomeEmail";
        saga.UpdatedAt = DateTime.UtcNow;

        // Write command to outbox: SendWelcomeEmail
        var outboxMessage = new TransactionalOutboxMessage
        {
            Id = Guid.NewGuid(),
            AggregateId = sagaId,
            EventType = "SendWelcomeEmail",
            Payload = JsonSerializer.Serialize(new
            {
                SagaId = sagaId,
                SubscriptionId = saga.SubscriptionId,
                TenantName = saga.TenantName,
                RecipientEmail = "customer@example.com" // TODO: Get from customer data
            }),
            CreatedAt = DateTime.UtcNow,
            IsProduced = false
        };

        _context.OutboxMessages.Add(outboxMessage);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("SAGA {SagaId} - tenant {TenantId} provisioned", sagaId, tenantId);
    }

    public async Task HandleTenantProvisioningFailedAsync(Guid sagaId, string errorMessage, CancellationToken cancellationToken = default)
    {
        var saga = await _context.SagaInstances.FindAsync(new object[] { sagaId }, cancellationToken);
        if (saga == null)
        {
            _logger.LogWarning("SAGA {SagaId} not found", sagaId);
            return;
        }

        saga.Status = "Compensating";
        saga.CurrentStep = "CompensateBilling";
        saga.ErrorMessage = errorMessage;
        saga.CompensationNeeded = true;
        saga.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogWarning("SAGA {SagaId} - provisioning failed: {Error}. Starting compensation.", sagaId, errorMessage);

        // Trigger compensation
        await CompensateSagaAsync(sagaId, cancellationToken);
    }

    public async Task HandleEmailSentAsync(Guid sagaId, Guid emailId, CancellationToken cancellationToken = default)
    {
        var saga = await _context.SagaInstances.FindAsync(new object[] { sagaId }, cancellationToken);
        if (saga == null)
        {
            _logger.LogWarning("SAGA {SagaId} not found", sagaId);
            return;
        }

        saga.EmailId = emailId;
        saga.Status = "Completed";
        saga.CurrentStep = "Finished";
        saga.CompletedAt = DateTime.UtcNow;
        saga.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("SAGA {SagaId} completed successfully", sagaId);
    }

    public async Task CompensateSagaAsync(Guid sagaId, CancellationToken cancellationToken = default)
    {
        var saga = await _context.SagaInstances.FindAsync(new object[] { sagaId }, cancellationToken);
        if (saga == null)
        {
            _logger.LogWarning("SAGA {SagaId} not found for compensation", sagaId);
            return;
        }

        if (!saga.SubscriptionId.HasValue)
        {
            // PIVOT not reached, no compensation needed
            saga.Status = "Failed";
            saga.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("SAGA {SagaId} failed before PIVOT - no compensation needed", sagaId);
            return;
        }

        // PIVOT reached - compensation required
        saga.Status = "Compensating";
        saga.CompensationNeeded = true;
        saga.UpdatedAt = DateTime.UtcNow;

        // 1. Deprovision tenant (if provisioned)
        if (saga.TenantId.HasValue)
        {
            var deprovisionMessage = new TransactionalOutboxMessage
            {
                Id = Guid.NewGuid(),
                AggregateId = sagaId,
                EventType = "DeprovisionTenant",
                Payload = JsonSerializer.Serialize(new
                {
                    SagaId = sagaId,
                    TenantId = saga.TenantId.Value
                }),
                CreatedAt = DateTime.UtcNow,
                IsProduced = false
            };
            _context.OutboxMessages.Add(deprovisionMessage);
        }

        // 2. Cancel subscription (always, since PIVOT reached)
        var cancelMessage = new TransactionalOutboxMessage
        {
            Id = Guid.NewGuid(),
            AggregateId = sagaId,
            EventType = "CancelSubscription",
            Payload = JsonSerializer.Serialize(new
            {
                SagaId = sagaId,
                SubscriptionId = saga.SubscriptionId.Value
            }),
            CreatedAt = DateTime.UtcNow,
            IsProduced = false
        };
        _context.OutboxMessages.Add(cancelMessage);

        saga.Status = "Compensated";
        saga.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("SAGA {SagaId} compensation commands issued", sagaId);
    }

    public async Task<List<SagaInstance>> GetAllSagasAsync(string? status = null, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        var query = _context.SagaInstances.AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(s => s.Status == status);
        }

        var sagas = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Retrieved {Count} SAGA instances with filter status={Status}", sagas.Count, status);
        return sagas;
    }
}

