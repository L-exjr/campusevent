namespace EventManagement.Api.Services;

public sealed class OperationalMetrics(TimeProvider timeProvider)
{
    private long paymentCallbacksSucceeded, paymentCallbacksFailed;
    private long emailsSent, emailsRetried, emailsFailed;
    private long storageCleanupSucceeded, storageCleanupFailed;
    private long providerQuotaWarnings;
    private readonly DateTimeOffset startedAt = timeProvider.GetUtcNow();

    public void PaymentCallback(bool succeeded) { if (succeeded) Interlocked.Increment(ref paymentCallbacksSucceeded); else Interlocked.Increment(ref paymentCallbacksFailed); }
    public void Email(string outcome) { if (outcome == "sent") Interlocked.Increment(ref emailsSent); else if (outcome == "failed") Interlocked.Increment(ref emailsFailed); else Interlocked.Increment(ref emailsRetried); }
    public void StorageCleanup(bool succeeded) { if (succeeded) Interlocked.Increment(ref storageCleanupSucceeded); else Interlocked.Increment(ref storageCleanupFailed); }
    public void ProviderQuotaWarning() => Interlocked.Increment(ref providerQuotaWarnings);

    public object Snapshot() => new
    {
        startedAt,
        paymentCallbacks = new { succeeded = Interlocked.Read(ref paymentCallbacksSucceeded), failed = Interlocked.Read(ref paymentCallbacksFailed) },
        emailDelivery = new { sent = Interlocked.Read(ref emailsSent), retried = Interlocked.Read(ref emailsRetried), failed = Interlocked.Read(ref emailsFailed) },
        storageCleanup = new { succeeded = Interlocked.Read(ref storageCleanupSucceeded), failed = Interlocked.Read(ref storageCleanupFailed) },
        providerQuotaWarnings = Interlocked.Read(ref providerQuotaWarnings)
    };
}
