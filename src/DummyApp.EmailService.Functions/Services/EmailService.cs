using Azure;
using Azure.Communication.Email;
using DummyApp.EmailService.Functions.Models;
using DummyApp.EmailService.Functions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Net;
using System.Text.Json;

namespace DummyApp.EmailService.Functions.Services;

public sealed class EmailService : IEmailService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
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
    }

    public async Task SendEmailAsync(SendEmailRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        if (string.IsNullOrWhiteSpace(request.Subject))
            throw new InvalidOperationException("Email subject must be provided.");

        if (request.Recipients is null || !request.Recipients.Any())
            throw new InvalidOperationException("At least one recipient must be provided.");

        if (request.Recipients.Any(string.IsNullOrWhiteSpace))
            throw new InvalidOperationException("Recipients must contain valid email addresses.");

        if (request.Template == EmailTemplate.Unknown)
            throw new InvalidOperationException("A valid email template must be selected.");

        if (request.Parameters is null)
            throw new InvalidOperationException("Template parameters are required.");

        var recipients = request.Recipients
            .Select(recipient => new EmailAddress(recipient.Trim()))
            .ToList();

        var body = GetBodyForTemplate(request.Template, request.Parameters.Value);

        var emailClient = new EmailClient(_options.ConnectionString);
        var recipientsContainer = new EmailRecipients(recipients);
        var content = new EmailContent(request.Subject)
        {
            PlainText = body,
            Html = $"<html><body><p>{WebUtility.HtmlEncode(body)}</p></body></html>"
        };

        var emailMessage = new EmailMessage(_options.SenderAddress, recipientsContainer, content);

        await emailClient.SendAsync(WaitUntil.Completed, emailMessage, cancellationToken);
    }

    private static string GetBodyForTemplate(EmailTemplate template, JsonElement parameters)
    {
        return template switch
        {
            EmailTemplate.Invite => GetBodyForInvite(parameters),
            _ => throw new InvalidOperationException($"Email template '{template}' is not supported.")
        };
    }

    private static string GetBodyForInvite(JsonElement parameters)
    {
        if (!parameters.TryGetProperty("token", out var tokenElement) || tokenElement.ValueKind != JsonValueKind.String)
            throw new InvalidOperationException("Template parameter 'token' is required for Invite.");

        var token = tokenElement.GetString();
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("Template parameter 'token' is required for Invite.");

        if (parameters.TryGetProperty("url", out var urlElement)
            && urlElement.ValueKind == JsonValueKind.String
            && !string.IsNullOrWhiteSpace(urlElement.GetString()))
        {
            return $"You are invited to DummyApp. Complete your invitation by visiting: {urlElement.GetString()}";
        }

        return $"You are invited to DummyApp. Use the following token to complete your invitation: {token}";
    }

    private static T DeserializeTemplateParameters<T>(JsonElement parameters, string templateName)
    {
        try
        {
            var typedParams = JsonSerializer.Deserialize<T>(parameters.GetRawText(), JsonOptions);
            if (typedParams is null)
                throw new InvalidOperationException($"Template parameters for {templateName} are invalid.");

            return typedParams;
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Template parameters for {templateName} are invalid.", ex);
        }
    }
}
