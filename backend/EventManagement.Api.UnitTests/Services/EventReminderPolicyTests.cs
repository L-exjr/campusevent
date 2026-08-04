using EventManagement.Api.Services;

namespace EventManagement.Api.UnitTests.Services;

public sealed class EventReminderPolicyTests
{
    private static readonly DateTimeOffset Now = new(2030, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(false, true, 12, false)]
    [InlineData(true, false, 12, false)]
    [InlineData(true, true, -1, false)]
    [InlineData(true, true, 12, true)]
    public void Discards_ineligible_reminders(
        bool published,
        bool active,
        int eventHoursFromNow,
        bool alreadySent)
    {
        var decision = EventReminderPolicy.Evaluate(
            published,
            active,
            Now.AddHours(eventHoursFromNow),
            alreadySent,
            Now,
            Now.AddHours(24));

        Assert.Equal(EventReminderDecision.Discard, decision);
    }

    [Fact]
    public void Defers_event_rescheduled_beyond_the_lead_window()
    {
        var decision = EventReminderPolicy.Evaluate(
            true, true, Now.AddHours(48), false, Now, Now.AddHours(24));

        Assert.Equal(EventReminderDecision.Defer, decision);
    }

    [Fact]
    public void Sends_eligible_upcoming_reminder()
    {
        var decision = EventReminderPolicy.Evaluate(
            true, true, Now.AddHours(12), false, Now, Now.AddHours(24));

        Assert.Equal(EventReminderDecision.Send, decision);
    }
}
