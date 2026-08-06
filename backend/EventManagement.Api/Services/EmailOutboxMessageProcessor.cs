using EventManagement.Api.Data;
using EventManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Api.Services;

public sealed class EmailOutboxMessageProcessor(
    AppDbContext dbContext,
    IEnumerable<IEmailOutboxHandler> handlers,
    IConfiguration configuration,
    TimeProvider timeProvider)
{
    public async Task ProcessAsync(
        EmailOutboxMessage claimedMessage,
        Guid claimId,
        CancellationToken cancellationToken)
    {
        var handler = handlers.SingleOrDefault(item => item.CanHandle(claimedMessage.Kind));
        var result = handler is null
            ? new EmailOutboxHandlingResult(
                EmailOutboxOutcome.Discard,
                $"Unsupported email outbox kind: {claimedMessage.Kind}.")
            : await handler.HandleAsync(claimedMessage, cancellationToken);
        var message = await dbContext.EmailOutboxMessages.SingleAsync(
            item => item.Id == claimedMessage.Id &&
                    item.ClaimedBy == claimId &&
                    item.Status == EmailOutboxStatus.Processing,
            cancellationToken);

        switch (result.Outcome)
        {
            case EmailOutboxOutcome.Sent:
                message.Status = EmailOutboxStatus.Sent;
                message.SentAt = timeProvider.GetUtcNow();
                message.PayloadJson = null;
                break;
            case EmailOutboxOutcome.Discard:
                message.Status = EmailOutboxStatus.Discarded;
                message.PayloadJson = null;
                break;
            case EmailOutboxOutcome.Defer:
                message.Status = EmailOutboxStatus.Pending;
                message.AvailableAt = result.AvailableAt ?? timeProvider.GetUtcNow().AddMinutes(1);
                break;
            case EmailOutboxOutcome.Retry:
                ApplyRetry(message);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        message.LastError = result.Error;
        message.ClaimedAt = null;
        message.ClaimedBy = null;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RecoverUnexpectedFailureAsync(
        Guid messageId,
        Guid claimId,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var message = await dbContext.EmailOutboxMessages.SingleOrDefaultAsync(
            item => item.Id == messageId &&
                    item.ClaimedBy == claimId &&
                    item.Status == EmailOutboxStatus.Processing,
            cancellationToken);
        if (message is null) return;

        ApplyRetry(message);
        var error = $"{exception.GetType().Name}: {exception.Message}";
        message.LastError = error[..Math.Min(error.Length, 2000)];
        message.ClaimedAt = null;
        message.ClaimedBy = null;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private void ApplyRetry(EmailOutboxMessage message)
    {
        var maxAttempts = Math.Max(configuration.GetValue("Email:Outbox:MaxAttempts", 8), 1);
        message.Status = message.AttemptCount >= maxAttempts
            ? EmailOutboxStatus.Failed
            : EmailOutboxStatus.Pending;
        message.AvailableAt = timeProvider.GetUtcNow().AddMinutes(
            Math.Min(Math.Pow(2, Math.Max(message.AttemptCount - 1, 0)), 60));
        if (message.Status == EmailOutboxStatus.Failed &&
            !EmailOutboxRecoveryPolicy.ShouldRetainPayloadOnFailure(message.Kind))
        {
            message.PayloadJson = null;
        }
    }
}
