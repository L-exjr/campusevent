using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Encodings.Web;

namespace EventManagement.Api.Services;

public sealed class MailtrapEmailService(
    IConfiguration configuration,
    IWebHostEnvironment environment,
    IHttpClientFactory httpClientFactory,
    ILogger<MailtrapEmailService> logger) : IEmailService
{
    private const string MailtrapEndpoint = "https://send.api.mailtrap.io/api/send";

    public async Task<bool> SendEmailAsync(
        string recipientEmail,
        string recipientName,
        string subject,
        string templateName,
        IReadOnlyDictionary<string, string?> templateValues,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var apiToken = GetConfigurationValue("Email:Api:Token", "MAILTRAP_API_TOKEN");
            var senderEmail = GetConfigurationValue(
                "Email:Api:SenderEmail",
                "MAILTRAP_SENDER_EMAIL");
            var senderName = GetConfigurationValue(
                "Email:Api:SenderName",
                "MAILTRAP_SENDER_NAME") ?? "Campus Events";

            if (string.IsNullOrWhiteSpace(apiToken) || string.IsNullOrWhiteSpace(senderEmail))
            {
                logger.LogError(
                    "Email was not sent to {Recipient}: Mailtrap API configuration is incomplete.",
                    recipientEmail);
                return false;
            }

            var body = await LoadTemplateAsync(templateName, templateValues, cancellationToken);
            using var request = new HttpRequestMessage(HttpMethod.Post, MailtrapEndpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);
            request.Content = JsonContent.Create(new
            {
                from = new { email = senderEmail, name = senderName },
                to = new[] { new { email = recipientEmail, name = recipientName } },
                subject,
                html = body,
                category = "Campus Events"
            });

            using var response = await httpClientFactory
                .CreateClient(nameof(MailtrapEmailService))
                .SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogError(
                    "Mailtrap rejected template {TemplateName} for {Recipient} with status {StatusCode}.",
                    templateName,
                    recipientEmail,
                    (int)response.StatusCode);
                return false;
            }

            logger.LogInformation(
                "Mailtrap accepted template {TemplateName} for {Recipient}.",
                templateName,
                recipientEmail);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(
                "Email send to {Recipient} was cancelled after the parent action completed.",
                recipientEmail);
            return false;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Email using template {TemplateName} could not be sent to {Recipient}.",
                templateName,
                recipientEmail);
            return false;
        }
    }

    private async Task<string> LoadTemplateAsync(
        string templateName,
        IReadOnlyDictionary<string, string?> values,
        CancellationToken cancellationToken)
    {
        var safeName = Path.GetFileName(templateName);
        if (!string.Equals(safeName, templateName, StringComparison.Ordinal))
            throw new InvalidOperationException("The email template name is invalid.");

        var path = Path.Combine(environment.ContentRootPath, "EmailTemplates", safeName);
        var html = await File.ReadAllTextAsync(path, cancellationToken);
        foreach (var (key, value) in values)
        {
            html = html.Replace(
                $"{{{{{key}}}}}",
                HtmlEncoder.Default.Encode(value ?? string.Empty),
                StringComparison.Ordinal);
        }

        return html;
    }

    private string? GetConfigurationValue(string sectionKey, string environmentKey) =>
        configuration[sectionKey] ?? configuration[environmentKey];
}
