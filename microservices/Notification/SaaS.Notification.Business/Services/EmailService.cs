using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SaaS.Notification.Repository.Contexts;
using SaaS.Notification.Shared.Entities;

namespace SaaS.Notification.Business.Services;

/// <summary>
/// Service for handling email operations with transactional outbox pattern
/// </summary>
public class EmailService : IEmailService
{
    private readonly NotificationDbContext _context;
    private readonly ILogger<EmailService> _logger;

    public EmailService(NotificationDbContext context, ILogger<EmailService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<Guid> SendWelcomeEmailAsync(Guid subscriptionId, string recipientEmail, string tenantName, CancellationToken cancellationToken = default)
    {
        var emailId = Guid.NewGuid();
        var emailLog = new EmailLog
        {
            Id = emailId,
            SubscriptionId = subscriptionId,
            RecipientEmail = recipientEmail,
            Subject = "Welcome to SaaS Platform!",
            Body = $"Welcome {tenantName}! Your subscription is now active.",
            Status = "pending",
            CreatedAt = DateTime.UtcNow
        };

        await _context.EmailLogs.AddAsync(emailLog, cancellationToken);

        // Simulate email sending (in real scenario this would call SMTP/SendGrid/etc)
        _logger.LogInformation("Simulating email sending to {Email}...", recipientEmail);
        await Task.Delay(200, cancellationToken); // Simulate network delay

        // Update status and write to outbox
        emailLog.Status = "sent";
        emailLog.SentAt = DateTime.UtcNow;

        var emailSentEvent = new EmailSentEvent(
            EmailId: emailId,
            SubscriptionId: subscriptionId,
            RecipientEmail: recipientEmail,
            EmailType: "welcome",
            SentAt: emailLog.SentAt.Value
        );

        var outboxMessage = new TransactionalOutboxMessage
        {
            Id = Guid.NewGuid(),
            AggregateId = emailId,
            EventType = "EmailSent",
            Payload = JsonSerializer.Serialize(emailSentEvent),
            IsProduced = false,
            CreatedAt = DateTime.UtcNow
        };

        await _context.OutboxMessages.AddAsync(outboxMessage, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Welcome email sent to {Email} for subscription {SubscriptionId}", recipientEmail, subscriptionId);
        return emailId;
    }

    /// <inheritdoc/>
    public async Task<Guid> SendInvoiceEmailAsync(Guid subscriptionId, string recipientEmail, decimal amount, CancellationToken cancellationToken = default)
    {
        var emailId = Guid.NewGuid();
        var emailLog = new EmailLog
        {
            Id = emailId,
            SubscriptionId = subscriptionId,
            RecipientEmail = recipientEmail,
            Subject = "Your Invoice",
            Body = $"Your invoice for ${amount:F2} is ready.",
            Status = "pending",
            CreatedAt = DateTime.UtcNow
        };

        await _context.EmailLogs.AddAsync(emailLog, cancellationToken);

        // Simulate email sending
        _logger.LogInformation("Simulating invoice email sending to {Email}...", recipientEmail);
        await Task.Delay(200, cancellationToken);

        // Update status and write to outbox
        emailLog.Status = "sent";
        emailLog.SentAt = DateTime.UtcNow;

        var emailSentEvent = new EmailSentEvent(
            EmailId: emailId,
            SubscriptionId: subscriptionId,
            RecipientEmail: recipientEmail,
            EmailType: "invoice",
            SentAt: emailLog.SentAt.Value
        );

        var outboxMessage = new TransactionalOutboxMessage
        {
            Id = Guid.NewGuid(),
            AggregateId = emailId,
            EventType = "EmailSent",
            Payload = JsonSerializer.Serialize(emailSentEvent),
            IsProduced = false,
            CreatedAt = DateTime.UtcNow
        };

        await _context.OutboxMessages.AddAsync(outboxMessage, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Invoice email sent to {Email} for subscription {SubscriptionId}", recipientEmail, subscriptionId);
        return emailId;
    }

    /// <inheritdoc/>
    public async Task<EmailLog?> GetEmailLogAsync(Guid emailId, CancellationToken cancellationToken = default)
    {
        return await _context.EmailLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == emailId, cancellationToken);
    }
}

/// <summary>
/// Event published when an email is successfully sent
/// </summary>
/// <param name="EmailId">The email log identifier</param>
/// <param name="SubscriptionId">The subscription identifier</param>
/// <param name="RecipientEmail">The recipient email address</param>
/// <param name="EmailType">The type of email (welcome, invoice, etc.)</param>
/// <param name="SentAt">When the email was sent</param>
public record EmailSentEvent(
    Guid EmailId,
    Guid SubscriptionId,
    string RecipientEmail,
    string EmailType,
    DateTime SentAt
);
