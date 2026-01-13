using Microsoft.AspNetCore.Mvc;
using SaaS.Provisioning.Business.Services;
using SaaS.Provisioning.Shared.DTOs;

namespace SaaS.Provisioning.Api.Controllers;

/// <summary>
/// Manages tenant provisioning and lifecycle operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class TenantsController : ControllerBase
{
    private readonly ITenantService _tenantService;
    private readonly ILogger<TenantsController> _logger;

    public TenantsController(ITenantService tenantService, ILogger<TenantsController> logger)
    {
        _tenantService = tenantService;
        _logger = logger;
    }

    /// <summary>
    /// Provisions a new tenant for a subscription
    /// </summary>
    /// <param name="request">Provision request with subscription ID and tenant name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Provisioned tenant details</returns>
    /// <response code="201">Tenant successfully provisioned</response>
    /// <response code="400">Invalid request</response>
    /// <response code="500">Provisioning failed after retries</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ProvisionTenantResponse>> ProvisionAsync(
        [FromBody] ProvisionTenantRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.TenantName))
        {
            return BadRequest("Tenant name cannot be empty");
        }

        try
        {
            _logger.LogInformation("Provisioning tenant for subscription {SubscriptionId}", request.SubscriptionId);

            var tenant = await _tenantService.ProvisionTenantAsync(
                request.SubscriptionId,
                request.TenantName,
                cancellationToken);

            var response = new ProvisionTenantResponse
            {
                TenantId = tenant.Id,
                SubscriptionId = tenant.SubscriptionId,
                TenantName = tenant.TenantName,
                Status = tenant.Status,
                ProvisionedAt = tenant.ProvisionedAt,
                CreatedAt = tenant.CreatedAt
            };

            return CreatedAtAction("Get", new { id = tenant.Id }, response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Provisioning failed for subscription {SubscriptionId}", request.SubscriptionId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves tenant details by ID
    /// </summary>
    /// <param name="id">Tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tenant details if found</returns>
    /// <response code="200">Tenant found</response>
    /// <response code="404">Tenant not found</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetTenantResponse>> GetAsync(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving tenant {TenantId}", id);

        var tenant = await _tenantService.GetTenantAsync(id, cancellationToken);

        if (tenant == null)
        {
            return NotFound($"Tenant {id} not found");
        }

        var response = new GetTenantResponse
        {
            TenantId = tenant.Id,
            SubscriptionId = tenant.SubscriptionId,
            TenantName = tenant.TenantName,
            Status = tenant.Status,
            ErrorMessage = tenant.ErrorMessage,
            ProvisioningAttempts = tenant.ProvisioningAttempts,
            ProvisionedAt = tenant.ProvisionedAt,
            CreatedAt = tenant.CreatedAt,
            UpdatedAt = tenant.UpdatedAt
        };

        return Ok(response);
    }

    /// <summary>
    /// Deprovisions an existing tenant
    /// </summary>
    /// <param name="id">Tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content if successful</returns>
    /// <response code="204">Tenant successfully deprovisioned</response>
    /// <response code="404">Tenant not found</response>
    [HttpDelete("{id:guid}/deprovision")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeprovisionAsync(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deprovisioning tenant {TenantId}", id);

        var success = await _tenantService.DeprovisionTenantAsync(id, cancellationToken);

        if (!success)
        {
            return NotFound($"Tenant {id} not found");
        }

        return NoContent();
    }
}
