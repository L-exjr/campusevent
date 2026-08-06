using EventManagement.Api.Data;
using EventManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Api.Services;

public sealed class BookingRequestRetentionService(
    AppDbContext dbContext,
    IConfiguration configuration,
    TimeProvider timeProvider)
{
    public async Task<int> ApplyAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var retentionDays = Math.Max(
            configuration.GetValue("DataRetention:BookingRequests:ClosedRetentionDays", 90),
            1);
        var cutoff = now.AddDays(-retentionDays);

        return await dbContext.BookingRequests
            .Where(request =>
                request.Status == BookingRequestStatus.Closed &&
                request.UpdatedAt < cutoff &&
                request.PersonalDataAnonymizedAt == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(request => request.ContactName, "Removed")
                .SetProperty(request => request.Email, "removed@invalid.local")
                .SetProperty(request => request.Phone, "Removed")
                .SetProperty(request => request.AlternativeDates, (string?)null)
                .SetProperty(request => request.FlexibilityNote, (string?)null)
                .SetProperty(request => request.PreferredOrganizer, (string?)null)
                .SetProperty(request => request.Description, "Personal data removed under the retention policy.")
                .SetProperty(request => request.OrganizerResponseNote, (string?)null)
                .SetProperty(request => request.PersonalDataAnonymizedAt, now),
                cancellationToken);
    }
}
