using DummyApp.EmailService.Functions.Models;

namespace DummyApp.EmailService.Functions.Services;

public sealed class EmailService : IEmailService
{
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
}
