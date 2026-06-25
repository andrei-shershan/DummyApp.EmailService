namespace DummyApp.EmailService.Functions.Options;

public sealed class KeyVaultOptions
{
    public const string SectionName = "KeyVault";

    public string? Url { get; init; }
}
