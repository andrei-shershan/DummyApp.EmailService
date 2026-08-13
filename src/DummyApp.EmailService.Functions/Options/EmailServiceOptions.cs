namespace DummyApp.EmailService.Functions.Options;

public sealed class EmailServiceOptions
{
    public const string SectionName = "EmailServiceOptions";

    public string? ConnectionString { get; init; }
    public string? SenderAddress { get; init; }
    public string? SenderDisplayName { get; init; }
    public string? RecipientAddress { get; init; }
    public string Subject { get; init; } = "DummyApp test email";
    public string Body { get; init; } = "This is a test email from DummyApp Email Service via Azure Communication Email.";
    public bool EnableOfflineDelivery { get; init; } = true;
}
