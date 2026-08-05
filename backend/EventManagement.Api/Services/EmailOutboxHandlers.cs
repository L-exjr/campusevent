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
        EmailOutbox.PasswordResetKind
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

public sealed class RegistrationConfirmationEmailOutboxHandler(
    AppDbContext dbContext,
    IEmailService emailService) : IEmailOutboxHandler
{
    public bool CanHandle(string kind) => kind == EmailOutbox.RegistrationConfirmationKind;

    public async Task<EmailOutboxHandlingResult> HandleAsync(
        EmailOutboxMessage message,
        CancellationToken cancellationToken)
    {
        var registration = await dbContext.EventRegistrations.AsNoTracking()
            .Where(item => item.Id == message.AggregateId)
            .Select(item => new
            {
                StudentIsActive = item.Student.IsActive,
                StudentEmail = item.Student.Email,
                StudentName = item.Student.Name,
                EventTitle = item.Event.Title,
                EventDate = item.Event.Date,
                EventLocation = item.Event.Location
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (!RegistrationConfirmationEmailPolicy.ShouldDeliver(
                registration is not null,
                registration?.StudentIsActive ?? false))
        {
            return new EmailOutboxHandlingResult(
                EmailOutboxOutcome.Discard,
                "The registration confirmation is no longer valid.");
        }

        var sent = await emailService.SendEmailAsync(
            registration!.StudentEmail,
            registration.StudentName,
            $"Registration confirmed: {registration.EventTitle}",
            "RegistrationConfirmation.html",
            new Dictionary<string, string?>
            {
                ["StudentName"] = registration.StudentName,
                ["EventTitle"] = registration.EventTitle,
                ["EventDate"] = registration.EventDate.ToString("f"),
                ["EventLocation"] = registration.EventLocation
            },
            cancellationToken);
        return sent
            ? new EmailOutboxHandlingResult(EmailOutboxOutcome.Sent)
            : new EmailOutboxHandlingResult(
                EmailOutboxOutcome.Retry,
                "The email provider did not accept the message.");
    }
}

public sealed class OrganizerApplicationDecisionEmailOutboxHandler(
    AppDbContext dbContext,
    IEmailService emailService) : IEmailOutboxHandler
{
    public bool CanHandle(string kind) => kind == EmailOutbox.OrganizerApplicationDecisionKind;

    public async Task<EmailOutboxHandlingResult> HandleAsync(
        EmailOutboxMessage message,
        CancellationToken cancellationToken)
    {
        var application = await dbContext.OrganizerApplications.AsNoTracking()
            .Where(item => item.Id == message.AggregateId)
            .Select(item => new
            {
                item.Status,
                item.RejectionReason,
                UserEmail = item.User.Email,
                UserName = item.User.Name
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (application is null || application.Status == ApplicationStatus.Pending)
            return new EmailOutboxHandlingResult(
                EmailOutboxOutcome.Discard,
                "The Organizer application has no final decision.");

        var decision = application.Status == ApplicationStatus.Approved ? "approved" : "rejected";
        var decisionDetails = application.Status == ApplicationStatus.Approved
            ? "You can sign in again to receive an Organizer access token."
            : string.IsNullOrWhiteSpace(application.RejectionReason)
                ? "Contact an administrator if you would like more information."
                : $"Reason: {application.RejectionReason}";
        var sent = await emailService.SendEmailAsync(
            application.UserEmail,
            application.UserName,
            $"Your Organizer application was {decision}",
            "OrganizerApplicationDecision.html",
            new Dictionary<string, string?>
            {
                ["StudentName"] = application.UserName,
                ["Decision"] = decision,
                ["DecisionDetails"] = decisionDetails
            },
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
