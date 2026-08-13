using DummyApp.EmailService.Functions.Options;
using DummyApp.EmailService.Functions.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DummyApp.EmailService.Functions.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEmailServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<EmailServiceOptions>()
            .Bind(configuration.GetSection(EmailServiceOptions.SectionName))
            .ValidateDataAnnotations();

        services.AddSingleton<IEmailService, DummyApp.EmailService.Functions.Services.EmailService>();
        return services;
    }
}
