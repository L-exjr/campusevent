using System.Text.Json;
using EventManagement.Api.Data;
using EventManagement.Api.Models;

namespace EventManagement.Api.Services;

public sealed record EmailOutboxPayload(
    string RecipientEmail,
    string RecipientName,
    string Subject,
    string TemplateName,
    IReadOnlyDictionary<string, string?> TemplateValues);

public static class EmailOutbox
{
    public const string EventReminderKind = "EventReminder";
    public const string RegistrationConfirmationKind = "RegistrationConfirmation";
    public const string PasswordResetKind = "PasswordReset";
    public const string OrganizerApplicationDecisionKind = "OrganizerApplicationDecision";

    public static void Enqueue(
        AppDbContext dbContext,
        string idempotencyKey,
        string kind,
        Guid aggregateId,
        EmailOutboxPayload payload)
    {
        dbContext.EmailOutboxMessages.Add(new EmailOutboxMessage
        {
            IdempotencyKey = idempotencyKey,
            Kind = kind,
            AggregateId = aggregateId,
            PayloadJson = JsonSerializer.Serialize(payload)
        });
    }

    public static void EnqueueDomainMessage(
        AppDbContext dbContext,
        string idempotencyKey,
        string kind,
        Guid aggregateId)
    {
        dbContext.EmailOutboxMessages.Add(new EmailOutboxMessage
        {
            IdempotencyKey = idempotencyKey,
            Kind = kind,
            AggregateId = aggregateId
        });
    }

    public static EmailOutboxPayload? Deserialize(string? payloadJson) =>
        string.IsNullOrWhiteSpace(payloadJson)
            ? null
            : JsonSerializer.Deserialize<EmailOutboxPayload>(payloadJson);
}
