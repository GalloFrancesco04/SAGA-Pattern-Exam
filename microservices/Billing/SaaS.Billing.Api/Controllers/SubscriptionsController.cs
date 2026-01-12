using Microsoft.AspNetCore.Mvc;
using SaaS.Billing.Business.Services;
using SaaS.Billing.Shared.Entities;

namespace SaaS.Billing.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SubscriptionsController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly ILogger<SubscriptionsController> _logger;

    public SubscriptionsController(
        ISubscriptionService subscriptionService,
        ILogger<SubscriptionsController> logger)
    {
        _subscriptionService = subscriptionService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new subscription for a customer
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Subscription), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSubscription(
        [FromBody] CreateSubscriptionRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating subscription for customer {CustomerId} with plan {PlanId}",
            request.CustomerId, request.PlanId);

        var subscription = await _subscriptionService.CreateSubscriptionAsync(
            request.CustomerId,
            request.PlanId,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetSubscription),
            new { id = subscription.Id },
            subscription);
    }

    /// <summary>
    /// Gets subscription details by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Subscription), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSubscription(
        Guid id,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving subscription {SubscriptionId}", id);

        var subscription = await _subscriptionService.GetSubscriptionAsync(id, cancellationToken);

        if (subscription == null)
        {
            return NotFound(new { message = $"Subscription {id} not found" });
        }

        return Ok(subscription);
    }

    /// <summary>
    /// Cancels an existing subscription
    /// </summary>
    [HttpDelete("{id}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelSubscription(
        Guid id,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Cancelling subscription {SubscriptionId}", id);

        var success = await _subscriptionService.CancelSubscriptionAsync(id, cancellationToken);

        if (!success)
        {
            return NotFound(new { message = $"Subscription {id} not found" });
        }

        return NoContent();
    }
}

/// <summary>
/// Request model for creating a subscription
/// </summary>
public record CreateSubscriptionRequest(
    Guid CustomerId,
    string PlanId);
