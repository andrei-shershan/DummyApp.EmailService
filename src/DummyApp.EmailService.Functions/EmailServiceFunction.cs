using System.Linq;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using DummyApp.EmailService.Functions.Models;
using DummyApp.EmailService.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace DummyApp.EmailService.Functions;

public sealed class EmailServiceFunction
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };
    private readonly IEmailService _emailService;
    private readonly ILogger<EmailServiceFunction> _logger;

    public EmailServiceFunction(IEmailService emailService, ILogger<EmailServiceFunction> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    [Function("SendEmail")]
    public async Task<HttpResponseData> Run(
#if DEBUG
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "email/send")]
    #else
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "email/send")]
    #endif
        HttpRequestData req,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("SendEmail triggered. Method: {Method}, Url: {Url}", req.Method, req.Url);
        _logger.LogInformation("************************************************");

        var body = await new StreamReader(req.Body).ReadToEndAsync(cancellationToken);
        _logger.LogInformation("Request Body: {Body}", body);

        SendEmailRequest? request;
        try
        {
            request = JsonSerializer.Deserialize<SendEmailRequest>(body, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Invalid JSON in send email request.");
            return CreateBadRequest(req, "Invalid JSON in request body.");
        }

        if (request is null)
        {
            _logger.LogWarning("SendEmail request body deserialized to null.");
            return CreateBadRequest(req, "Request body is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Subject))
            return CreateBadRequest(req, "Subject is required.");

        if (request.Recipients is null || request.Recipients.Count == 0)
            return CreateBadRequest(req, "At least one recipient is required.");

        if (request.Recipients.Any(string.IsNullOrWhiteSpace))
            return CreateBadRequest(req, "Recipients must contain valid email addresses.");

        if (request.Template == EmailTemplate.Unknown)
            return CreateBadRequest(req, "A valid template is required.");

        if (request.Parameters is null)
            return CreateBadRequest(req, "Template parameters are required.");

        await _emailService.SendEmailAsync(request, cancellationToken);

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new { message = "Email request accepted." }, cancellationToken);
        return response;
    }


    private static HttpResponseData CreateBadRequest(HttpRequestData req, string message)
    {
        var response = req.CreateResponse(HttpStatusCode.BadRequest);
        response.WriteString(message);
        return response;
    }
}
