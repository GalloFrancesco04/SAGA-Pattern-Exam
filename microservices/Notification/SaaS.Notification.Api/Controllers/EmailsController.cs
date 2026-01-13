using Microsoft.AspNetCore.Mvc;
using SaaS.Notification.Business.Services;
using SaaS.Notification.Shared.DTOs;

namespace SaaS.Notification.Api.Controllers;

/// <summary>
/// Controller for email operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class EmailsController : ControllerBase
{
    private readonly IEmailService _emailService;
    private readonly ILogger<EmailsController> _logger;

    public EmailsController(IEmailService emailService, ILogger<EmailsController> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    /// <summary>
    /// Sends a welcome email for a new subscription
    /// </summary>
    /// <param name="request">The welcome email request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The email send response</returns>
    [HttpPost("welcome")]
    [ProducesResponseType(typeof(SendEmailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SendWelcomeEmailAsync([FromBody] SendWelcomeEmailRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var emailId = await _emailService.SendWelcomeEmailAsync(
                request.SubscriptionId,
                request.RecipientEmail,
                request.TenantName,
                cancellationToken
            );

            var emailLog = await _emailService.GetEmailLogAsync(emailId, cancellationToken);
            if (emailLog == null)
            {
                return StatusCode(500, "Failed to retrieve email log after sending");
            }

            var response = new SendEmailResponse(
                EmailId: emailLog.Id,
                SubscriptionId: emailLog.SubscriptionId,
                RecipientEmail: emailLog.RecipientEmail,
                Status: emailLog.Status,
                SentAt: emailLog.SentAt
            );

            return CreatedAtAction("GetEmailLog", new { id = emailId }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending welcome email for subscription {SubscriptionId}", request.SubscriptionId);
            return StatusCode(500, "Failed to send welcome email");
        }
    }

    /// <summary>
    /// Sends an invoice email for a subscription
    /// </summary>
    /// <param name="request">The invoice email request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The email send response</returns>
    [HttpPost("invoice")]
    [ProducesResponseType(typeof(SendEmailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SendInvoiceEmailAsync([FromBody] SendInvoiceEmailRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var emailId = await _emailService.SendInvoiceEmailAsync(
                request.SubscriptionId,
                request.RecipientEmail,
                request.Amount,
                cancellationToken
            );

            var emailLog = await _emailService.GetEmailLogAsync(emailId, cancellationToken);
            if (emailLog == null)
            {
                return StatusCode(500, "Failed to retrieve email log after sending");
            }

            var response = new SendEmailResponse(
                EmailId: emailLog.Id,
                SubscriptionId: emailLog.SubscriptionId,
                RecipientEmail: emailLog.RecipientEmail,
                Status: emailLog.Status,
                SentAt: emailLog.SentAt
            );

            return CreatedAtAction("GetEmailLog", new { id = emailId }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending invoice email for subscription {SubscriptionId}", request.SubscriptionId);
            return StatusCode(500, "Failed to send invoice email");
        }
    }

    /// <summary>
    /// Gets an email log by identifier
    /// </summary>
    /// <param name="id">The email log identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The email log response</returns>
    [HttpGet("{id:guid}", Name = "GetEmailLog")]
    [ProducesResponseType(typeof(GetEmailLogResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEmailLogAsync([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var emailLog = await _emailService.GetEmailLogAsync(id, cancellationToken);
        if (emailLog == null)
        {
            return NotFound();
        }

        var response = new GetEmailLogResponse(
            EmailId: emailLog.Id,
            SubscriptionId: emailLog.SubscriptionId,
            RecipientEmail: emailLog.RecipientEmail,
            Subject: emailLog.Subject,
            Status: emailLog.Status,
            CreatedAt: emailLog.CreatedAt,
            SentAt: emailLog.SentAt
        );

        return Ok(response);
    }
}
