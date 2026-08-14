using DummyApp.EmailService.Functions.Models;

namespace DummyApp.EmailService.Functions.Services;

public interface IEmailService
{
    Task SendEmailAsync(SendEmailRequest request, CancellationToken cancellationToken);
}
