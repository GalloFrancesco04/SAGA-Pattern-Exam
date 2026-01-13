namespace SaaS.Notification.Business.Services;

/// <summary>
/// Service interface for email operations
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends a welcome email for a new subscription
    /// </summary>
    /// <param name="subscriptionId">The subscription identifier</param>
    /// <param name="recipientEmail">The recipient email address</param>
    /// <param name="tenantName">The tenant name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The email log identifier</returns>
    Task<Guid> SendWelcomeEmailAsync(Guid subscriptionId, string recipientEmail, string tenantName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an invoice email for a subscription
    /// </summary>
    /// <param name="subscriptionId">The subscription identifier</param>
    /// <param name="recipientEmail">The recipient email address</param>
    /// <param name="amount">The invoice amount</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The email log identifier</returns>
    Task<Guid> SendInvoiceEmailAsync(Guid subscriptionId, string recipientEmail, decimal amount, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an email log by identifier
    /// </summary>
    /// <param name="emailId">The email log identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The email log or null if not found</returns>
    Task<SaaS.Notification.Shared.Entities.EmailLog?> GetEmailLogAsync(Guid emailId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all email logs with optional filtering and pagination
    /// </summary>
    /// <param name="status">Filter by email status (optional)</param>
    /// <param name="skip">Number of records to skip (pagination)</param>
    /// <param name="take">Number of records to take (pagination)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of email logs</returns>
    Task<List<SaaS.Notification.Shared.Entities.EmailLog>> GetAllEmailLogsAsync(string? status = null, int skip = 0, int take = 50, CancellationToken cancellationToken = default);
}
