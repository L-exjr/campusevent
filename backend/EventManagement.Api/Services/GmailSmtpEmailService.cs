using System.Net;
using System.Net.Mail;

namespace EventManagement.Api.Services;

public sealed class GmailSmtpEmailService(
    IConfiguration configuration,
    EmailTemplateRenderer templateRenderer,
    EmailDailySendMonitor dailySendMonitor,
    ILogger<GmailSmtpEmailService> logger) : IEmailService
{
    public async Task<bool> SendEmailAsync(
        string recipientEmail,
        string recipientName,
        string subject,
        string templateName,
        IReadOnlyDictionary<string, string?> templateValues,
        CancellationToken cancellationToken = default)
    {
        var username = GetConfigurationValue("Email:Gmail:Username", "GMAIL_SMTP_USERNAME");
        var appPassword = GetConfigurationValue("Email:Gmail:AppPassword", "GMAIL_APP_PASSWORD");
        var senderEmail = GetConfigurationValue("Email:Gmail:SenderEmail", "GMAIL_SENDER_EMAIL") ?? username;
        var senderName = GetConfigurationValue("Email:Gmail:SenderName", "GMAIL_SENDER_NAME") ?? "Campus Events";
        var host = GetConfigurationValue("Email:Gmail:Host", "GMAIL_SMTP_HOST") ?? "smtp.gmail.com";
        var port = GetPositiveInteger("Email:Gmail:Port", "GMAIL_SMTP_PORT", 587);
        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(appPassword) ||
            string.IsNullOrWhiteSpace(senderEmail))
        {
            logger.LogError(
                "Email was not sent to {Recipient}: Gmail SMTP configuration is incomplete.",
                recipientEmail);
            return false;
        }

        try
        {
            var body = await templateRenderer.RenderAsync(
                templateName,
                templateValues,
                cancellationToken);
            using var message = new MailMessage
            {
                From = new MailAddress(senderEmail, senderName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            message.To.Add(new MailAddress(recipientEmail, recipientName));
            using var client = new SmtpClient(host, port)
            {
                EnableSsl = true,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(username, appPassword),
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = 30_000
            };
            await client.SendMailAsync(message).WaitAsync(cancellationToken);
            dailySendMonitor.RecordAccepted();
            logger.LogInformation(
                "Gmail SMTP accepted template {TemplateName} for {Recipient}.",
                templateName,
                recipientEmail);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Email send to {Recipient} was cancelled.", recipientEmail);
            return false;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Gmail SMTP could not send template {TemplateName} to {Recipient}.",
                templateName,
                recipientEmail);
            return false;
        }
    }

    private string? GetConfigurationValue(string sectionKey, string environmentKey)
    {
        var environmentValue = configuration[environmentKey];
        return string.IsNullOrWhiteSpace(environmentValue)
            ? configuration[sectionKey]
            : environmentValue;
    }

    private int GetPositiveInteger(string sectionKey, string environmentKey, int fallback)
    {
        var value = GetConfigurationValue(sectionKey, environmentKey);
        return int.TryParse(value, out var parsed) && parsed > 0 ? parsed : fallback;
    }
}
