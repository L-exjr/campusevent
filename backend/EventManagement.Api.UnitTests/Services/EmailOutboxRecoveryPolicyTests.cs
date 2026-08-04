using EventManagement.Api.Services;

namespace EventManagement.Api.UnitTests.Services;

public sealed class EmailOutboxRecoveryPolicyTests
{
    [Fact]
    public void Password_reset_payload_is_not_retained_or_retryable()
    {
        Assert.False(EmailOutboxRecoveryPolicy.ShouldRetainPayloadOnFailure(
            EmailOutbox.PasswordResetKind));
        Assert.False(EmailOutboxRecoveryPolicy.CanRetry(
            EmailOutbox.PasswordResetKind,
            hasPayload: true));
    }

    [Theory]
    [InlineData(EmailOutbox.RegistrationConfirmationKind)]
    [InlineData(EmailOutbox.OrganizerApplicationDecisionKind)]
    public void Non_sensitive_payload_messages_can_be_retried_when_payload_exists(string kind)
    {
        Assert.True(EmailOutboxRecoveryPolicy.ShouldRetainPayloadOnFailure(kind));
        Assert.True(EmailOutboxRecoveryPolicy.CanRetry(kind, hasPayload: true));
        Assert.False(EmailOutboxRecoveryPolicy.CanRetry(kind, hasPayload: false));
    }

    [Fact]
    public void Event_reminder_can_be_regenerated_from_domain_state() =>
        Assert.True(EmailOutboxRecoveryPolicy.CanRetry(
            EmailOutbox.EventReminderKind,
            hasPayload: false));
}
