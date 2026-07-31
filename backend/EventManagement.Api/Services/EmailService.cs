using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mail;
using System.Text.Encodings.Web;

namespace EventManagement.Api.Services;

public sealed class EmailService(
    IConfiguration configuration,
    IWebHostEnvironment environment,
    IHttpClientFactory httpClientFactory,
    ILogger<EmailService> logger) : IEmailService
{
    private const string MailtrapSandboxHost = "sandbox.smtp.mailtrap.io";

    public async Task<bool> SendAsync(
        string recipientEmail,
        string recipientName,
        string subject,
        string templateName,
        IReadOnlyDictionary<string, string?> templateValues,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var host = configuration["Email:Smtp:Host"];
            var username = configuration["Email:Smtp:Username"];
            var password = configuration["Email:Smtp:Password"];
            var fromAddress = configuration["Email:Smtp:FromAddress"];
            var fromName = configuration["Email:Smtp:FromName"] ?? "Campus Events";
            var port = configuration.GetValue<int?>("Email:Smtp:Port") ?? 587;
            var enableSsl = configuration.GetValue("Email:Smtp:EnableSsl", true);
            var apiToken = configuration["Email:Api:Token"];

            if (string.IsNullOrWhiteSpace(fromAddress))
            {
                logger.LogWarning(
                    "Email was not sent to {Recipient}: sender configuration is incomplete.",
                    recipientEmail);
                return false;
            }

            var body = await LoadTemplateAsync(templateName, templateValues, cancellationToken);
            if (!string.IsNullOrWhiteSpace(apiToken))
            {
                return await SendWithApiAsync(
                    apiToken,
                    fromAddress,
                    fromName,
                    recipientEmail,
                    recipientName,
                    subject,
                    body,
                    templateName,
                    cancellationToken);
            }

            if (string.IsNullOrWhiteSpace(host) ||
                string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password))
            {
                logger.LogWarning(
                    "Email was not sent to {Recipient}: SMTP configuration is incomplete.",
                    recipientEmail);
                return false;
            }

            // Development is intentionally locked to Mailtrap so a local mistake cannot
            // deliver test messages to real inboxes.
            if (environment.IsDevelopment() &&
                !string.Equals(host, MailtrapSandboxHost, StringComparison.OrdinalIgnoreCase))
            {
                logger.LogError(
                    "Email was not sent to {Recipient}: Development SMTP host must be {MailtrapHost}.",
                    recipientEmail,
                    MailtrapSandboxHost);
                return false;
            }

            using var message = new MailMessage
            {
                From = new MailAddress(fromAddress, fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            message.To.Add(new MailAddress(recipientEmail, recipientName));

            using var client = new SmtpClient(host, port)
            {
                EnableSsl = enableSsl,
                Credentials = new NetworkCredential(username, password)
            };
            await client.SendMailAsync(message, cancellationToken);
            logger.LogInformation(
                "Email using template {TemplateName} was sent to {Recipient}.",
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

    private async Task<bool> SendWithApiAsync(
        string apiToken,
        string fromAddress,
        string fromName,
        string recipientEmail,
        string recipientName,
        string subject,
        string html,
        string templateName,
        CancellationToken cancellationToken)
    {
        var useSandbox = configuration.GetValue("Email:Api:UseSandbox", true);
        var inboxId = configuration.GetValue<long?>("Email:Api:InboxId");
        if (useSandbox && (!inboxId.HasValue || inboxId <= 0))
        {
            logger.LogWarning(
                "Email was not sent to {Recipient}: Email:Api:InboxId is required in sandbox mode.",
                recipientEmail);
            return false;
        }

        var endpoint = useSandbox
            ? $"https://sandbox.api.mailtrap.io/api/send/{inboxId}"
            : "https://send.api.mailtrap.io/api/send";
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        if (useSandbox)
            request.Headers.Add("Api-Token", apiToken);
        else
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);
        request.Content = JsonContent.Create(new
        {
            from = new { email = fromAddress, name = fromName },
            to = new[] { new { email = recipientEmail, name = recipientName } },
            subject,
            html
        });

        using var response = await httpClientFactory.CreateClient().SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError(
                "Email API rejected template {TemplateName} for {Recipient} with status {StatusCode}.",
                templateName,
                recipientEmail,
                (int)response.StatusCode);
            return false;
        }

        logger.LogInformation(
            "Email API delivered template {TemplateName} to {Recipient}.",
            templateName,
            recipientEmail);
        return true;
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
}
