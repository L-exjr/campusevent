namespace EventManagement.Api.Services;

/// <summary>
/// Emits a structured count of SMTP messages accepted by Gmail in this process.
/// This is deliberately lightweight: restarts reset the count and multiple replicas
/// each maintain their own count. Use a shared metric before scaling horizontally.
/// </summary>
public sealed class EmailDailySendMonitor(
    IConfiguration configuration,
    TimeProvider timeProvider,
    ILogger<EmailDailySendMonitor> logger)
{
    private readonly object sync = new();
    private DateOnly utcDay = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
    private int acceptedCount;

    public void RecordAccepted()
    {
        int count;
        int warningThreshold;
        DateOnly day;
        lock (sync)
        {
            day = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
            if (day != utcDay)
            {
                utcDay = day;
                acceptedCount = 0;
            }

            count = ++acceptedCount;
            warningThreshold = GetWarningThreshold();
        }

        logger.LogInformation(
            "Gmail daily send count: {AcceptedCount} messages accepted on {UtcDay} UTC for this application process.",
            count,
            day);
        if (count == warningThreshold || count > warningThreshold && count % 25 == 0)
        {
            logger.LogWarning(
                "Gmail daily send warning: {AcceptedCount} messages accepted on {UtcDay} UTC in this process; configured warning threshold is {WarningThreshold}.",
                count,
                day,
                warningThreshold);
        }
    }

    private int GetWarningThreshold()
    {
        var environmentValue = configuration["GMAIL_DAILY_WARNING_THRESHOLD"];
        var configured = int.TryParse(environmentValue, out var environmentThreshold)
            ? environmentThreshold
            : configuration.GetValue("Email:Gmail:DailyWarningThreshold", 400);
        return Math.Clamp(configured, 1, 500);
    }
}
