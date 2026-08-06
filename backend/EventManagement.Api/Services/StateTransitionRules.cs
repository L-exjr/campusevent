using EventManagement.Api.Infrastructure;
using EventManagement.Api.Models;
using System.Reflection;
using System.Text.Json;

namespace EventManagement.Api.Services;

public static class StateTransitionRules
{
    private static readonly IReadOnlyDictionary<BookingRequestStatus, IReadOnlySet<BookingRequestStatus>>
        BookingTransitions = LoadBookingTransitions();

    public static void EnsureBookingTransition(
        BookingRequestStatus current,
        BookingRequestStatus target)
    {
        if (BookingTransitions[current].Contains(target)) return;
        throw new ApiException(
            StatusCodes.Status409Conflict,
            $"A booking request cannot move from {current} to {target}.");
    }

    public static void EnsureEventPublicationTransition(
        bool? currentlyPublished,
        bool targetPublished,
        DateTimeOffset currentDate,
        DateTimeOffset targetDate,
        DateTimeOffset now)
    {
        if (!targetPublished) return;

        // Existing historical events remain editable when their date is unchanged,
        // but a draft cannot be published in the past and an event cannot be moved
        // into the past while remaining published.
        var isNewOrDraft = currentlyPublished != true;
        var dateChanged = targetDate != currentDate;
        if (targetDate <= now && (isNewOrDraft || dateChanged))
        {
            throw new ApiException(
                StatusCodes.Status400BadRequest,
                "Published events must be scheduled in the future.");
        }
    }

    private static IReadOnlyDictionary<BookingRequestStatus, IReadOnlySet<BookingRequestStatus>>
        LoadBookingTransitions()
    {
        var assembly = typeof(StateTransitionRules).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .Single(name => name.EndsWith("booking-transitions.json", StringComparison.Ordinal));
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException("The booking transition contract is missing.");
        var contract = JsonSerializer.Deserialize<Dictionary<string, string[]>>(stream)
            ?? throw new InvalidOperationException("The booking transition contract is invalid.");
        var transitions = new Dictionary<BookingRequestStatus, IReadOnlySet<BookingRequestStatus>>();
        foreach (var (sourceName, targetNames) in contract)
        {
            if (!Enum.TryParse<BookingRequestStatus>(sourceName, true, out var source))
                throw new InvalidOperationException($"Unknown booking status '{sourceName}'.");
            var targets = targetNames.Select(targetName =>
                Enum.TryParse<BookingRequestStatus>(targetName, true, out var target)
                    ? target
                    : throw new InvalidOperationException($"Unknown booking status '{targetName}'."));
            transitions[source] = targets.ToHashSet();
        }
        if (transitions.Count != Enum.GetValues<BookingRequestStatus>().Length)
            throw new InvalidOperationException("The booking transition contract must define every status.");
        return transitions;
    }
}
