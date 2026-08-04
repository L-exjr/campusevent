namespace EventManagement.Api.Services;

public interface IEmailService
{
    Task<bool> SendEmailAsync(
        string recipientEmail,
        string recipientName,
        string subject,
        string templateName,
        IReadOnlyDictionary<string, string?> templateValues,
        CancellationToken cancellationToken = default);
}
