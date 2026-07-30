using System.Net;
using System.Net.Mail;
using System.Text.Encodings.Web;

namespace EventManagement.Api.Services;

public sealed class EmailService(
    IConfiguration configuration,
    IWebHostEnvironment environment,
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

            if (string.IsNullOrWhiteSpace(host) ||
                string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(fromAddress))
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

            var body = await LoadTemplateAsync(templateName, templateValues, cancellationToken);
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
