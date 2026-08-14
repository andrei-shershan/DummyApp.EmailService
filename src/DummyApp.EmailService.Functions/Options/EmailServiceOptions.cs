namespace DummyApp.EmailService.Functions.Options;

public sealed class EmailServiceOptions
{
    public const string SectionName = "EmailServiceOptions";

    public string? ConnectionString { get; init; }
    public string? SenderAddress { get; init; }
    public string? SenderDisplayName { get; init; }
    public bool EnableOfflineDelivery { get; init; } = true;
}
