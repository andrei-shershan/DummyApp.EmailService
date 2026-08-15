using System.Text.Json;

namespace DummyApp.EmailService.Functions.Models;

public sealed class SendEmailRequest
{
    public string Subject { get; init; } = string.Empty;
    public IReadOnlyCollection<string> Recipients { get; init; } = Array.Empty<string>();
    public EmailTemplate Template { get; init; } = EmailTemplate.Unknown;
    public JsonElement? Parameters { get; init; }
    public IReadOnlyCollection<SendEmailAttachment> Attachments { get; init; } = Array.Empty<SendEmailAttachment>();
}
