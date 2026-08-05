namespace EventManagement.Api.Services;

public static class EmailOutboxRecoveryPolicy
{
    public static bool ShouldRetainPayloadOnFailure(string kind) => false;

    public static bool CanRetry(string kind) => kind switch
    {
        EmailOutbox.EventReminderKind or
        EmailOutbox.RegistrationConfirmationKind or
            EmailOutbox.OrganizerApplicationDecisionKind => true,
        _ => false
    };
}
