namespace SaaS.Notification.Shared.DTOs;

/// <summary>
/// Request to send a welcome email
/// </summary>
/// <param name="SubscriptionId">The subscription identifier</param>
/// <param name="RecipientEmail">The recipient email address</param>
/// <param name="TenantName">The tenant name</param>
public record SendWelcomeEmailRequest(
    Guid SubscriptionId,
    string RecipientEmail,
    string TenantName
);

/// <summary>
/// Request to send an invoice email
/// </summary>
/// <param name="SubscriptionId">The subscription identifier</param>
/// <param name="RecipientEmail">The recipient email address</param>
/// <param name="Amount">The invoice amount</param>
public record SendInvoiceEmailRequest(
    Guid SubscriptionId,
    string RecipientEmail,
    decimal Amount
);

/// <summary>
/// Response for email send operations
/// </summary>
/// <param name="EmailId">The email log identifier</param>
/// <param name="SubscriptionId">The subscription identifier</param>
/// <param name="RecipientEmail">The recipient email address</param>
/// <param name="Status">The email status</param>
/// <param name="SentAt">When the email was sent (null if pending)</param>
public record SendEmailResponse(
    Guid EmailId,
    Guid SubscriptionId,
    string RecipientEmail,
    string Status,
    DateTime? SentAt
);

/// <summary>
/// Response for getting an email log
/// </summary>
/// <param name="EmailId">The email log identifier</param>
/// <param name="SubscriptionId">The subscription identifier</param>
/// <param name="RecipientEmail">The recipient email address</param>
/// <param name="Subject">The email subject</param>
/// <param name="Status">The email status</param>
/// <param name="CreatedAt">When the email was created</param>
/// <param name="SentAt">When the email was sent (null if not sent)</param>
public record GetEmailLogResponse(
    Guid EmailId,
    Guid SubscriptionId,
    string RecipientEmail,
    string Subject,
    string Status,
    DateTime CreatedAt,
    DateTime? SentAt
);
