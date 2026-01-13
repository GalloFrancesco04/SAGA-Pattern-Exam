using Microsoft.AspNetCore.Mvc;
using SaaS.Orchestrator.Business.Services;
using SaaS.Orchestrator.Shared.DTOs;

namespace SaaS.Orchestrator.Api.Controllers;

/// <summary>
/// Controller for managing SAGA orchestration
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class SagasController : ControllerBase
{
    private readonly ISagaService _sagaService;
    private readonly ILogger<SagasController> _logger;

    public SagasController(ISagaService sagaService, ILogger<SagasController> logger)
    {
        _sagaService = sagaService;
        _logger = logger;
    }

    /// <summary>
    /// Starts a new subscription SAGA
    /// </summary>
    [HttpPost("subscription")]
    [ProducesResponseType(typeof(StartSagaResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<StartSagaResponse>> StartSubscriptionSagaAsync(
        [FromBody] StartSubscriptionSagaRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.PlanId) || string.IsNullOrWhiteSpace(request.TenantName))
        {
            return BadRequest("PlanId and TenantName are required");
        }

        _logger.LogInformation("Starting subscription SAGA for customer {CustomerId}", request.CustomerId);

        var sagaId = await _sagaService.StartSubscriptionSagaAsync(
            request.CustomerId,
            request.PlanId,
            request.TenantName,
            cancellationToken
        );

        var saga = await _sagaService.GetSagaInstanceAsync(sagaId, cancellationToken);
        if (saga == null)
        {
            return StatusCode(500, "Failed to retrieve created SAGA");
        }

        var response = new StartSagaResponse(
            saga.Id,
            saga.Status,
            saga.CurrentStep,
            saga.CreatedAt
        );

        return CreatedAtAction(
            nameof(GetSagaStatusAsync),
            new { id = sagaId },
            response
        );
    }

    /// <summary>
    /// Gets the current status of a SAGA
    /// </summary>
    [HttpGet("{id:guid}", Name = "GetSagaStatus")]
    [ProducesResponseType(typeof(GetSagaStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetSagaStatusResponse>> GetSagaStatusAsync(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving SAGA status for {SagaId}", id);

        var saga = await _sagaService.GetSagaInstanceAsync(id, cancellationToken);
        if (saga == null)
        {
            return NotFound($"SAGA {id} not found");
        }

        var response = new GetSagaStatusResponse(
            saga.Id,
            saga.CustomerId,
            saga.PlanId,
            saga.TenantName,
            saga.SubscriptionId,
            saga.TenantId,
            saga.EmailId,
            saga.Status,
            saga.CurrentStep,
            saga.ErrorMessage,
            saga.CompensationNeeded,
            saga.CreatedAt,
            saga.UpdatedAt,
            saga.CompletedAt
        );

        return Ok(response);
    }

    /// <summary>
    /// Manually triggers compensation for a SAGA
    /// </summary>
    [HttpPost("{id:guid}/compensate")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompensateSagaAsync(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Manual compensation triggered for SAGA {SagaId}", id);

        var saga = await _sagaService.GetSagaInstanceAsync(id, cancellationToken);
        if (saga == null)
        {
            return NotFound($"SAGA {id} not found");
        }

        await _sagaService.CompensateSagaAsync(id, cancellationToken);

        return Accepted();
    }
}
