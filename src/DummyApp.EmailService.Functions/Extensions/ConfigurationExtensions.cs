using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using DummyApp.EmailService.Functions.Options;
using Microsoft.Extensions.Configuration;

namespace DummyApp.EmailService.Functions.Extensions;

public static class ConfigurationExtensions
{
    public static IConfigurationBuilder AddKeyVaultFromConfiguration(this IConfigurationBuilder configuration)
    {
        configuration.AddEnvironmentVariables();

        var builtConfig = configuration.Build();
        var keyVaultUrl = builtConfig[$"{KeyVaultOptions.SectionName}:{nameof(KeyVaultOptions.Url)}"];
        if (string.IsNullOrEmpty(keyVaultUrl))
        {
            return configuration;
        }

        var clientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");
        var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            ManagedIdentityClientId = string.IsNullOrWhiteSpace(clientId) ? null : clientId
        });

        configuration.AddAzureKeyVault(new Uri(keyVaultUrl), credential);
        return configuration;
    }
}
