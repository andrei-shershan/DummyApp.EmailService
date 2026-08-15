using Azure;
using Azure.Communication.Email;
using Azure.Core;
using DummyApp.EmailService.Functions.Models;
using DummyApp.EmailService.Functions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Net;
using System.Text;
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

        var body = BuildBodyForTemplate(request.Template, request.Parameters.Value);
        var htmlBody = BuildHtmlBodyForTemplate(request.Template, request.Parameters.Value);

        Console.WriteLine("Email Body: " + body);

        var emailClient = new EmailClient(_options.ConnectionString);
        var recipientsContainer = new EmailRecipients(recipients);
        var content = new EmailContent(request.Subject)
        {
            PlainText = body,
            Html = htmlBody
        };

        var emailMessage = new EmailMessage(_options.SenderAddress, recipientsContainer, content);

        if (request.Attachments is not null && request.Attachments.Any())
        {
            foreach (var attachment in request.Attachments)
            {
                if (string.IsNullOrWhiteSpace(attachment.Name) || string.IsNullOrWhiteSpace(attachment.ContentType) || string.IsNullOrWhiteSpace(attachment.Base64Content))
                {
                    throw new InvalidOperationException("Attachment name, content type, and base64 content must be provided.");
                }

                var binaryData = new BinaryData(Convert.FromBase64String(attachment.Base64Content));
                emailMessage.Attachments.Add(new EmailAttachment(attachment.Name, attachment.ContentType, binaryData));
            }
        }

        await emailClient.SendAsync(WaitUntil.Completed, emailMessage, cancellationToken);
    }

    public static string BuildBodyForTemplate(EmailTemplate template, JsonElement parameters)
    {
        return template switch
        {
            EmailTemplate.Invite => GetBodyForInvite(parameters),
            EmailTemplate.CompletedOrder => GetBodyForCompletedOrder(parameters),
            _ => throw new InvalidOperationException($"Email template '{template}' is not supported.")
        };
    }

    private static string BuildHtmlBodyForTemplate(EmailTemplate template, JsonElement parameters)
    {
        return template switch
        {
            EmailTemplate.Invite => GetHtmlBodyForInvite(parameters),
            EmailTemplate.CompletedOrder => GetHtmlBodyForCompletedOrderHtml(parameters),
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

    private static string GetHtmlBodyForInvite(JsonElement parameters)
    {
        var inviteText = GetBodyForInvite(parameters);
        return $"<html><body><p>{WebUtility.HtmlEncode(inviteText)}</p></body></html>";
    }

    private static string GetBodyForCompletedOrder(JsonElement parameters)
    {
        var templateParameters = DeserializeTemplateParameters<CompletedOrderTemplateParameters>(parameters, nameof(EmailTemplate.CompletedOrder));

        var builder = new StringBuilder();
        builder.AppendLine($"Order status: {templateParameters.Status}");
        builder.AppendLine();
        builder.AppendLine("Items:");

        foreach (var item in templateParameters.Items)
        {
            builder.AppendLine($"- {item.Name} x{item.Quantity} ({item.PrintSizeName}) - {item.PriceValue:C}");
            builder.AppendLine($"  Artwork ID: {item.ArtworkId}");
            builder.AppendLine($"  Description: {item.Description}");
        }

        builder.AppendLine();

        if (templateParameters.Address is not null)
        {
            builder.AppendLine("Shipping address:");
            builder.AppendLine($"{templateParameters.Address.FirstName} {templateParameters.Address.LastName}");
            builder.AppendLine($"{templateParameters.Address.Street} {templateParameters.Address.HouseNumber}");
            builder.AppendLine($"{templateParameters.Address.City}, {templateParameters.Address.PostalCode}");
            builder.AppendLine($"{templateParameters.Address.Country}");
            builder.AppendLine($"Email: {templateParameters.Address.Email}");
            builder.AppendLine($"Phone: {templateParameters.Address.Phone}");
        }
        else
        {
            builder.AppendLine("Shipping address: not provided.");
        }

        return builder.ToString();
    }

    private static string GetHtmlBodyForCompletedOrderHtml(JsonElement parameters)
    {
        var templateParameters = DeserializeTemplateParameters<CompletedOrderTemplateParameters>(parameters, nameof(EmailTemplate.CompletedOrder));

        var builder = new StringBuilder();
        builder.Append("<html><body style=\"font-family:Segoe UI,Arial,sans-serif;color:#111;line-height:1.5;margin:0;padding:0;\">");
        builder.Append("<div style=\"padding:24px;max-width:680px;margin:0 auto;background:#ffffff;\">");
        builder.Append("<h1 style=\"font-size:26px;margin-bottom:0.5rem;\">Order Summary</h1>");
        builder.Append("<div style=\"display:flex;flex-wrap:wrap;gap:1.5rem;margin-bottom:1.5rem;\">");

        if (!string.IsNullOrWhiteSpace(templateParameters.QrCodeBase64))
        {
            Console.WriteLine("QR Code Base64: " + templateParameters.QrCodeBase64);
            var qrCodeUri = templateParameters.QrCodeBase64.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase)
                ? templateParameters.QrCodeBase64
                : $"data:image/png;base64,{templateParameters.QrCodeBase64}";

            builder.Append("<div style=\"min-width:220px;max-width:240px;\">");
            builder.Append("<div style=\"font-weight:700;margin-bottom:0.5rem;\">QR Code</div>");
            builder.Append($"<img src=\"{qrCodeUri}\" alt=\"Order QR Code\" style=\"width:100%;height:auto;border:1px solid #ddd;border-radius:8px;padding:8px;\" />");
            builder.Append("</div>");
        }

        builder.Append("<div style=\"flex:1;min-width:220px;\">\n");
        builder.Append("<div style=\"font-weight:700;font-size:18px;margin-bottom:0.5rem;\">Order Details</div>");
        builder.Append($"<div style=\"margin-bottom:0.75rem;\">Status: <strong>{WebUtility.HtmlEncode(templateParameters.Status)}</strong></div>");

        if (templateParameters.Address is not null)
        {
            builder.Append("<div style=\"font-weight:700;margin-bottom:0.35rem;\">Delivery Address</div>");
            builder.Append($"<div>{WebUtility.HtmlEncode(templateParameters.Address.FirstName)} {WebUtility.HtmlEncode(templateParameters.Address.LastName)}</div>");
            builder.Append($"<div>{WebUtility.HtmlEncode(templateParameters.Address.Email)}</div>");
            builder.Append($"<div>{WebUtility.HtmlEncode(templateParameters.Address.Phone)}</div>");
            builder.Append($"<div>{WebUtility.HtmlEncode(templateParameters.Address.Street)} {WebUtility.HtmlEncode(templateParameters.Address.HouseNumber)}</div>");
            builder.Append($"<div>{WebUtility.HtmlEncode(templateParameters.Address.PostalCode)} {WebUtility.HtmlEncode(templateParameters.Address.City)}</div>");
            builder.Append($"<div>{WebUtility.HtmlEncode(templateParameters.Address.Country)}</div>");
        }
        else
        {
            builder.Append("<div>Shipping address: not provided.</div>");
        }

        builder.Append("</div>");
        builder.Append("</div>");
        builder.Append("<div style=\"font-weight:700;font-size:18px;margin-bottom:0.75rem;\">Order Items</div>");
        builder.Append("<table style=\"width:100%;border-collapse:collapse;\">\n");
        builder.Append("<thead><tr style=\"background:#f4f4f4;text-align:left;\">\n");
        builder.Append("<th style=\"padding:12px;border:1px solid #e1e1e1;\">Item</th>\n");
        builder.Append("<th style=\"padding:12px;border:1px solid #e1e1e1;\">Description</th>\n");
        builder.Append("<th style=\"padding:12px;border:1px solid #e1e1e1;\">Quantity</th>\n");
        builder.Append("<th style=\"padding:12px;border:1px solid #e1e1e1;\">Size</th>\n");
        builder.Append("<th style=\"padding:12px;border:1px solid #e1e1e1;\">Price</th>\n");
        builder.Append("</tr></thead>\n");
        builder.Append("<tbody>\n");

        foreach (var item in templateParameters.Items)
        {
            builder.Append("<tr>\n");
            builder.Append($"<td style=\"padding:12px;border:1px solid #e1e1e1;\">{WebUtility.HtmlEncode(item.Name)}</td>\n");
            builder.Append($"<td style=\"padding:12px;border:1px solid #e1e1e1;\">{WebUtility.HtmlEncode(item.Description)}</td>\n");
            builder.Append($"<td style=\"padding:12px;border:1px solid #e1e1e1;\">{item.Quantity}</td>\n");
            builder.Append($"<td style=\"padding:12px;border:1px solid #e1e1e1;\">{WebUtility.HtmlEncode(item.PrintSizeName)}</td>\n");
            builder.Append($"<td style=\"padding:12px;border:1px solid #e1e1e1;\">{item.PriceValue:C}</td>\n");
            builder.Append("</tr>\n");
        }

        builder.Append("</tbody>\n");
        builder.Append("</table>\n");
        builder.Append("</div></body></html>");

        return builder.ToString();
    }

    private sealed record CompletedOrderTemplateParameters
    {
        public IEnumerable<CompletedOrderTemplateItem> Items { get; init; } = Array.Empty<CompletedOrderTemplateItem>();
        public string Status { get; init; } = string.Empty;
        public CompletedOrderTemplateAddress? Address { get; init; }
        public string? QrCodeBase64 { get; init; }
    }

    private sealed record CompletedOrderTemplateItem
    {
        public Guid OrderId { get; init; }
        public Guid ArtworkId { get; init; }
        public int Quantity { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public string ImgUrl { get; init; } = string.Empty;
        public string ThumbnailUrl { get; init; } = string.Empty;
        public int? PrintSizeId { get; init; }
        public string PrintSizeName { get; init; } = string.Empty;
        public int? PriceId { get; init; }
        public decimal? PriceValue { get; init; }
    }

    private sealed record CompletedOrderTemplateAddress
    {
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public string Phone { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string Country { get; init; } = string.Empty;
        public string City { get; init; } = string.Empty;
        public string Street { get; init; } = string.Empty;
        public string HouseNumber { get; init; } = string.Empty;
        public string PostalCode { get; init; } = string.Empty;
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
