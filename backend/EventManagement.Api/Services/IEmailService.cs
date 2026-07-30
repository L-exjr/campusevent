namespace EventManagement.Api.Services;

public interface IEmailService
{
    Task<bool> SendAsync(
        string recipientEmail,
        string recipientName,
        string subject,
        string templateName,
        IReadOnlyDictionary<string, string?> templateValues,
        CancellationToken cancellationToken = default);
}
