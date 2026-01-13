namespace SaaS.Provisioning.Shared.DTOs;

/// <summary>
/// Request to provision a new tenant
/// </summary>
public record ProvisionTenantRequest
{
    /// <summary>
    /// Subscription ID associated with the tenant
    /// </summary>
    public Guid SubscriptionId { get; init; }

    /// <summary>
    /// Name of the tenant to provision
    /// </summary>
    public string TenantName { get; init; } = string.Empty;
}

/// <summary>
/// Response after provisioning a tenant
/// </summary>
public record ProvisionTenantResponse
{
    /// <summary>
    /// Unique identifier of the provisioned tenant
    /// </summary>
    public Guid TenantId { get; init; }

    /// <summary>
    /// Associated subscription ID
    /// </summary>
    public Guid SubscriptionId { get; init; }

    /// <summary>
    /// Name of the provisioned tenant
    /// </summary>
    public string TenantName { get; init; } = string.Empty;

    /// <summary>
    /// Current status (e.g., "active", "provisioning", "failed")
    /// </summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>
    /// Timestamp when provisioning was completed
    /// </summary>
    public DateTime? ProvisionedAt { get; init; }

    /// <summary>
    /// Timestamp when the tenant was created
    /// </summary>
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// Response when retrieving tenant details
/// </summary>
public record GetTenantResponse
{
    /// <summary>
    /// Unique identifier of the tenant
    /// </summary>
    public Guid TenantId { get; init; }

    /// <summary>
    /// Associated subscription ID
    /// </summary>
    public Guid SubscriptionId { get; init; }

    /// <summary>
    /// Name of the tenant
    /// </summary>
    public string TenantName { get; init; } = string.Empty;

    /// <summary>
    /// Current status (e.g., "active", "provisioning", "failed", "deprovisioning", "deprovisioned")
    /// </summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>
    /// Error message if provisioning failed
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Number of provisioning attempts made
    /// </summary>
    public int ProvisioningAttempts { get; init; }

    /// <summary>
    /// Timestamp when provisioning was completed
    /// </summary>
    public DateTime? ProvisionedAt { get; init; }

    /// <summary>
    /// Timestamp when the tenant was created
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Timestamp of the last update
    /// </summary>
    public DateTime UpdatedAt { get; init; }
}
