namespace DummyApp.EmailService.Functions.Models;

public sealed class InviteEmailRequest
{
    public string Email { get; init; } = string.Empty;
    public string Token { get; init; } = string.Empty;
}
