namespace EventManagement.Api.Services;

public enum EventReminderDecision
{
    Send,
    Defer,
    Discard
}

public static class EventReminderPolicy
{
    public static EventReminderDecision Evaluate(
        bool eventPublished,
        bool studentActive,
        DateTimeOffset eventDate,
        bool reminderAlreadySent,
        DateTimeOffset now,
        DateTimeOffset reminderCutoff)
    {
        if (reminderAlreadySent || !eventPublished || !studentActive || eventDate <= now)
            return EventReminderDecision.Discard;
        return eventDate > reminderCutoff
            ? EventReminderDecision.Defer
            : EventReminderDecision.Send;
    }
}
