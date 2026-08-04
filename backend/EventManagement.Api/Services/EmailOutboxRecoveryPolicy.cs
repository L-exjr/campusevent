namespace EventManagement.Api.Services;

public static class EmailOutboxRecoveryPolicy
{
    public static bool ShouldRetainPayloadOnFailure(string kind) =>
        kind is EmailOutbox.RegistrationConfirmationKind or
            EmailOutbox.OrganizerApplicationDecisionKind;

    public static bool CanRetry(string kind, bool hasPayload) => kind switch
    {
        EmailOutbox.EventReminderKind => true,
        EmailOutbox.RegistrationConfirmationKind or
            EmailOutbox.OrganizerApplicationDecisionKind => hasPayload,
        _ => false
    };
}
