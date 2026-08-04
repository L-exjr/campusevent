using System.Text.Json;
using EventManagement.Api.Data;
using EventManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Api.Services;

public enum EmailOutboxOutcome
{
    Sent,
    Retry,
    Discard,
    Defer
}

public sealed record EmailOutboxHandlingResult(
    EmailOutboxOutcome Outcome,
    string? Error = null,
    DateTimeOffset? AvailableAt = null);

public interface IEmailOutboxHandler
{
    bool CanHandle(string kind);
    Task<EmailOutboxHandlingResult> HandleAsync(
        EmailOutboxMessage message,
        CancellationToken cancellationToken);
}

public sealed class PayloadEmailOutboxHandler(
    AppDbContext dbContext,
    IEmailService emailService,
    ILogger<PayloadEmailOutboxHandler> logger) : IEmailOutboxHandler
{
    private static readonly IReadOnlySet<string> SupportedKinds = new HashSet<string>
    {
        EmailOutbox.PasswordResetKind,
        EmailOutbox.RegistrationConfirmationKind,
        EmailOutbox.OrganizerApplicationDecisionKind
    };

    public bool CanHandle(string kind) => SupportedKinds.Contains(kind);

    public async Task<EmailOutboxHandlingResult> HandleAsync(
        EmailOutboxMessage message,
        CancellationToken cancellationToken)
    {
        if (message.Kind == EmailOutbox.PasswordResetKind)
        {
            var resetToken = await dbContext.PasswordResetTokens.AsNoTracking()
                .Where(token => token.Id == message.AggregateId)
                .Select(token => new { token.ExpiresAt, token.UsedAt, token.User.IsActive })
                .SingleOrDefaultAsync(cancellationToken);
            if (resetToken is null || !PasswordResetEmailPolicy.ShouldDeliver(
                    resetToken.IsActive,
                    resetToken.UsedAt,
                    resetToken.ExpiresAt,
                    DateTimeOffset.UtcNow))
            {
                return new EmailOutboxHandlingResult(
                    EmailOutboxOutcome.Discard,
                    "The password reset token is no longer valid.");
            }
        }

        EmailOutboxPayload? payload;
        try
        {
            payload = EmailOutbox.Deserialize(message.PayloadJson);
        }
        catch (JsonException exception)
        {
            logger.LogError(exception, "Email outbox message {MessageId} has an invalid payload.", message.Id);
            return new EmailOutboxHandlingResult(
                EmailOutboxOutcome.Discard,
                "The email payload is invalid.");
        }
        if (payload is null)
            return new EmailOutboxHandlingResult(EmailOutboxOutcome.Discard, "The email payload is missing.");

        var sent = await emailService.SendEmailAsync(
            payload.RecipientEmail,
            payload.RecipientName,
            payload.Subject,
            payload.TemplateName,
            payload.TemplateValues,
            cancellationToken);
        return sent
            ? new EmailOutboxHandlingResult(EmailOutboxOutcome.Sent)
            : new EmailOutboxHandlingResult(
                EmailOutboxOutcome.Retry,
                "The email provider did not accept the message.");
    }
}

public sealed class EventReminderEmailOutboxHandler(
    AppDbContext dbContext,
    IEmailService emailService,
    IConfiguration configuration) : IEmailOutboxHandler
{
    public bool CanHandle(string kind) => kind == EmailOutbox.EventReminderKind;

    public async Task<EmailOutboxHandlingResult> HandleAsync(
        EmailOutboxMessage message,
        CancellationToken cancellationToken)
    {
        var registration = await dbContext.EventRegistrations
            .Include(item => item.Event)
            .Include(item => item.Student)
            .SingleOrDefaultAsync(item => item.Id == message.AggregateId, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var leadTime = TimeSpan.FromHours(Math.Max(
            configuration.GetValue("Email:Reminders:LeadTimeHours", 24),
            1));
        var decision = registration is null
            ? EventReminderDecision.Discard
            : EventReminderPolicy.Evaluate(
                registration.Event.IsPublished,
                registration.Student.IsActive,
                registration.Event.Date,
                registration.ReminderSentAt.HasValue,
                now,
                now.Add(leadTime));

        if (registration is null || decision == EventReminderDecision.Discard)
            return new EmailOutboxHandlingResult(
                EmailOutboxOutcome.Discard,
                "The registration no longer requires a reminder.");
        if (decision == EventReminderDecision.Defer)
            return new EmailOutboxHandlingResult(
                EmailOutboxOutcome.Defer,
                AvailableAt: registration.Event.Date - leadTime);

        var sent = await emailService.SendEmailAsync(
            registration.Student.Email,
            registration.Student.Name,
            $"Reminder: {registration.Event.Title} starts soon",
            "EventReminder.html",
            new Dictionary<string, string?>
            {
                ["StudentName"] = registration.Student.Name,
                ["EventTitle"] = registration.Event.Title,
                ["EventDate"] = registration.Event.Date.ToString("f"),
                ["EventLocation"] = registration.Event.Location
            },
            cancellationToken);
        if (!sent)
            return new EmailOutboxHandlingResult(
                EmailOutboxOutcome.Retry,
                "The email provider did not accept the message.");

        registration.ReminderSentAt = DateTimeOffset.UtcNow;
        return new EmailOutboxHandlingResult(EmailOutboxOutcome.Sent);
    }
}
