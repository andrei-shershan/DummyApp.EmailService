using Azure;
using Azure.Communication.Email;
using DummyApp.EmailService.Functions.Models;
using DummyApp.EmailService.Functions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;

namespace DummyApp.EmailService.Functions.Services;

public sealed class EmailService : IEmailService
{
    private readonly EmailServiceOptions _options;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailServiceOptions> emailServiceOptions, ILogger<EmailService> logger)
    {
        _options = emailServiceOptions?.Value ?? throw new ArgumentNullException(nameof(emailServiceOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (string.IsNullOrWhiteSpace(_options.ConnectionString))
            throw new InvalidOperationException("EmailServiceOptions.ConnectionString must be configured.");

        if (string.IsNullOrWhiteSpace(_options.SenderAddress))
            throw new InvalidOperationException("EmailServiceOptions.SenderAddress must be configured.");

        if (string.IsNullOrWhiteSpace(_options.RecipientAddress))
            throw new InvalidOperationException("EmailServiceOptions.RecipientAddress must be configured.");
    }

    public Task SendEmailAsync(SendEmailRequest request, CancellationToken cancellationToken)
    {
        // Mock implementation: just log or store the request in future.
        return Task.CompletedTask;
    }

    public Task SendInviteAsync(InviteEmailRequest request, CancellationToken cancellationToken)
    {
        // Mock implementation: in production, send an email with the invite token.
        return Task.CompletedTask;
    }

    public async Task SendTestEmailAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Sending test email from {Sender} to {Recipient}", _options.SenderAddress, _options.RecipientAddress);

        var emailClient = new EmailClient(_options.ConnectionString);
        var recipients = new EmailRecipients(new[] { new EmailAddress(_options.RecipientAddress) });

        var content = new EmailContent(_options.Subject)
        {
            PlainText = _options.Body,
            Html = $"<html><body><p>{WebUtility.HtmlEncode(_options.Body)}</p></body></html>"
        };

        var emailMessage = new EmailMessage(_options.SenderAddress, recipients, content);

        await emailClient.SendAsync(WaitUntil.Completed, emailMessage, cancellationToken);
    }
}
