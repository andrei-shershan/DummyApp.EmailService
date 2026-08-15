using System;
using System.Text.Json;
using System.Threading;
using DummyApp.EmailService.Functions.Models;
using DummyApp.EmailService.Functions.Options;
using DummyApp.EmailService.Functions.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace DummyApp.EmailService.Functions.Tests;

public sealed class EmailServiceCompletedOrderTests
{
    private static readonly ILogger<DummyApp.EmailService.Functions.Services.EmailService> Logger = NullLogger<DummyApp.EmailService.Functions.Services.EmailService>.Instance;

    private DummyApp.EmailService.Functions.Services.EmailService CreateService()
        => new DummyApp.EmailService.Functions.Services.EmailService(Microsoft.Extensions.Options.Options.Create(new EmailServiceOptions
        {
            ConnectionString = "Endpoint=https://example.com/;AccessKey=secret",
            SenderAddress = "noreply@example.com"
        }), Logger);

    [Fact]
    public void BuildBodyForCompletedOrder_ReturnsText_WhenParametersAreValid()
    {
        var body = JsonSerializer.SerializeToElement(new
        {
            Items = new[]
            {
                new
                {
                    OrderId = Guid.NewGuid(),
                    ArtworkId = Guid.NewGuid(),
                    Quantity = 2,
                    Name = "test",
                    Description = "test",
                    ImgUrl = "https://example.com/img.png",
                    ThumbnailUrl = "https://example.com/thumb.png",
                    PrintSizeId = 2,
                    PrintSizeName = "A2",
                    PriceId = 2,
                    PriceValue = 80.00m
                }
            },
            Status = "Completed",
            Address = new
            {
                FirstName = "Andrei",
                LastName = "Smith",
                Phone = "101202303",
                Email = "mail.shershan@gmail.com",
                Country = "Poland",
                City = "Warszawa",
                Street = "Dobra",
                HouseNumber = "1",
                PostalCode = "123465"
            }
        });

        var result = DummyApp.EmailService.Functions.Services.EmailService.BuildBodyForTemplate(EmailTemplate.CompletedOrder, body);

        Assert.Contains("Order status: Completed", result);
        Assert.Contains("test x2 (A2) -", result);
        Assert.Contains("Shipping address:", result);
        Assert.Contains("Andrei Smith", result);
        Assert.Contains("mail.shershan@gmail.com", result);
    }

    [Fact]
    public void SendEmailAsync_Throws_WhenParametersMissing()
    {
        var service = CreateService();
        var request = new SendEmailRequest
        {
            Subject = "Order Completed",
            Recipients = new[] { "mail.shershan@gmail.com" },
            Template = EmailTemplate.CompletedOrder,
            Parameters = null
        };

        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => service.SendEmailAsync(request, CancellationToken.None));
        Assert.Equal("Template parameters are required.", ex.Result.Message);
    }
}
