using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SaaS.Provisioning.Repository.Contexts;
using SaaS.Provisioning.Shared.Entities;
using SaaS.Provisioning.Shared;

namespace SaaS.Provisioning.Business.Services;

public class TenantService : ITenantService
{
    private readonly ProvisioningDbContext _context;
    private const int MaxRetries = 3;
    private readonly int[] _retryDelaysMs = { 1000, 2000, 4000 }; // exponential backoff: 1s, 2s, 4s

    public TenantService(ProvisioningDbContext context)
    {
        _context = context;
    }

    public async Task<Tenant> ProvisionTenantAsync(Guid subscriptionId, string tenantName, CancellationToken cancellationToken = default)
    {
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            SubscriptionId = subscriptionId,
            TenantName = tenantName,
            Status = "provisioning",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        Exception? lastException = null;

        // Retry logic: 3 attempts with exponential backoff
        for (int attempt = 0; attempt < MaxRetries; attempt++)
        {
            try
            {
                // Simulate provisioning logic (would call Azure/AWS APIs)
                await SimulateProvisioningAsync(tenant, cancellationToken);

                // If successful, mark as active
                tenant.Status = "active";
                tenant.ProvisionedAt = DateTime.UtcNow;
                tenant.ProvisioningAttempts = attempt + 1;
                tenant.ErrorMessage = null;

                // Save to DB with outbox event
                var outboxMessage = new TransactionalOutboxMessage
                {
                    Id = Guid.NewGuid(),
                    AggregateId = tenant.Id,
                    EventType = ProvisioningTopicNames.TenantProvisioned,
                    Payload = JsonSerializer.Serialize(new TenantProvisionedEvent
                    {
                        TenantId = tenant.Id,
                        SubscriptionId = tenant.SubscriptionId,
                        TenantName = tenant.TenantName,
                        Status = tenant.Status,
                        ProvisionedAt = tenant.ProvisionedAt.Value
                    }),
                    CreatedAt = DateTime.UtcNow
                };

                await _context.Tenants.AddAsync(tenant, cancellationToken);
                await _context.OutboxMessages.AddAsync(outboxMessage, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                return tenant;
            }
            catch (Exception ex)
            {
                lastException = ex;
                tenant.ProvisioningAttempts = attempt + 1;

                if (attempt < MaxRetries - 1)
                {
                    // Wait before retry
                    await Task.Delay(_retryDelaysMs[attempt], cancellationToken);
                }
            }
        }

        // All retries failed
        tenant.Status = "failed";
        tenant.ErrorMessage = lastException?.Message ?? "Provisioning failed after all retries";
        tenant.ProvisioningAttempts = MaxRetries;

        var failedOutboxMessage = new TransactionalOutboxMessage
        {
            Id = Guid.NewGuid(),
            AggregateId = tenant.Id,
            EventType = ProvisioningTopicNames.TenantProvisioningFailed,
            Payload = JsonSerializer.Serialize(new TenantProvisioningFailedEvent
            {
                TenantId = tenant.Id,
                SubscriptionId = tenant.SubscriptionId,
                TenantName = tenant.TenantName,
                ErrorMessage = tenant.ErrorMessage,
                Attempts = tenant.ProvisioningAttempts
            }),
            CreatedAt = DateTime.UtcNow
        };

        await _context.Tenants.AddAsync(tenant, cancellationToken);
        await _context.OutboxMessages.AddAsync(failedOutboxMessage, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        throw new InvalidOperationException($"Tenant provisioning failed after {MaxRetries} attempts: {lastException?.Message}");
    }

    public async Task<Tenant?> GetTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);
    }

    public async Task<bool> DeprovisionTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

        if (tenant == null)
        {
            return false;
        }

        if (string.Equals(tenant.Status, "deprovisioned", StringComparison.OrdinalIgnoreCase))
        {
            return true; // idempotent
        }

        tenant.Status = "deprovisioning";
        tenant.UpdatedAt = DateTime.UtcNow;

        // Simulate deprovision cleanup
        await SimulateDeprovisioningAsync(tenant, cancellationToken);

        tenant.Status = "deprovisioned";
        tenant.UpdatedAt = DateTime.UtcNow;

        var outboxMessage = new TransactionalOutboxMessage
        {
            Id = Guid.NewGuid(),
            AggregateId = tenant.Id,
            EventType = ProvisioningTopicNames.TenantDeprovisioned,
            Payload = JsonSerializer.Serialize(new TenantDeprovisionedEvent
            {
                TenantId = tenant.Id,
                SubscriptionId = tenant.SubscriptionId,
                TenantName = tenant.TenantName,
                Status = tenant.Status,
                DeprovisionedAt = tenant.UpdatedAt
            }),
            CreatedAt = DateTime.UtcNow
        };

        _context.OutboxMessages.Add(outboxMessage);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    /// <summary>
    /// Simulates Azure/AWS provisioning logic (can fail randomly for testing)
    /// </summary>
    private async Task SimulateProvisioningAsync(Tenant tenant, CancellationToken cancellationToken)
    {
        // Simulate network latency
        await Task.Delay(500, cancellationToken);

        // For demo: fail if attempt count is odd (to test retry logic)
        if (tenant.ProvisioningAttempts % 2 == 0 && tenant.ProvisioningAttempts > 0)
        {
            throw new InvalidOperationException("Simulated provisioning failure (will retry)");
        }
    }

    /// <summary>
    /// Simulates deprovision cleanup
    /// </summary>
    private async Task SimulateDeprovisioningAsync(Tenant tenant, CancellationToken cancellationToken)
    {
        // Simulate cleanup
        await Task.Delay(300, cancellationToken);
    }
}

public record TenantProvisionedEvent
{
    public Guid TenantId { get; init; }
    public Guid SubscriptionId { get; init; }
    public string TenantName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime ProvisionedAt { get; init; }
}

public record TenantProvisioningFailedEvent
{
    public Guid TenantId { get; init; }
    public Guid SubscriptionId { get; init; }
    public string TenantName { get; init; } = string.Empty;
    public string ErrorMessage { get; init; } = string.Empty;
    public int Attempts { get; init; }
}

public record TenantDeprovisionedEvent
{
    public Guid TenantId { get; init; }
    public Guid SubscriptionId { get; init; }
    public string TenantName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime DeprovisionedAt { get; init; }
}
