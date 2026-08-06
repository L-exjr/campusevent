namespace EventManagement.Api.Services;

public sealed class DataRetentionBackgroundService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<DataRetentionBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RunCycleAsync(stoppingToken);
        var interval = TimeSpan.FromHours(Math.Max(
            configuration.GetValue("DataRetention:SweepIntervalHours", 24), 1));
        using var timer = new PeriodicTimer(interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
            await RunCycleAsync(stoppingToken);
    }

    private async Task RunCycleAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<BookingRequestRetentionService>()
                .ApplyAsync(cancellationToken);
            await scope.ServiceProvider.GetRequiredService<AdminAuditRetentionService>()
                .ApplyAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception exception)
        {
            logger.LogError(exception, "The data-retention sweep failed; it will retry later.");
        }
    }
}
