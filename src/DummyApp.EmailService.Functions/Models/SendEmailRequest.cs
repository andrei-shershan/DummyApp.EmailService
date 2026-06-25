namespace DummyApp.EmailService.Functions.Models;

public sealed class SendEmailRequest
{
    public string Email { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
}
