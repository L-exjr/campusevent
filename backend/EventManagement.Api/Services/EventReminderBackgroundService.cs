using EventManagement.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Api.Services;

public sealed class EventReminderBackgroundService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<EventReminderBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(
            configuration.GetValue("Email:Reminders:CheckIntervalMinutes", 60));

        await CheckForRemindersAsync(stoppingToken);
        using var timer = new PeriodicTimer(interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await CheckForRemindersAsync(stoppingToken);
        }
    }

    private async Task CheckForRemindersAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
            var now = DateTimeOffset.UtcNow;
            var reminderCutoff = now.AddHours(
                configuration.GetValue("Email:Reminders:LeadTimeHours", 24));

            var registrations = await dbContext.EventRegistrations
                .Include(registration => registration.Event)
                .Include(registration => registration.Student)
                .Where(registration =>
                    registration.ReminderSentAt == null &&
                    registration.Event.Date > now &&
                    registration.Event.Date <= reminderCutoff)
                .OrderBy(registration => registration.Event.Date)
                .ToListAsync(cancellationToken);

            foreach (var registration in registrations)
            {
                var sent = await emailService.SendAsync(
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
                if (sent) registration.ReminderSentAt = DateTimeOffset.UtcNow;
            }

            if (dbContext.ChangeTracker.HasChanges())
                await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Normal host shutdown.
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "The event reminder check failed; it will retry on the next interval.");
        }
    }
}
