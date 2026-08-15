using EventManagement.Api.Data;
using EventManagement.Api.DTOs.Common;
using EventManagement.Api.DTOs.Organizers;
using EventManagement.Api.Infrastructure;
using EventManagement.Api.Mappings;
using EventManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Api.Services;

public interface IOrganizerDirectoryService
{
    Task<PaginatedResponse<PublicOrganizerSummary>> GetPublicAsync(string? search, string? category, int page, int pageSize, CancellationToken cancellationToken);
    Task<PublicOrganizerDetail> GetPublicByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<OrganizerDirectorySettings> GetSettingsAsync(Guid organizerId, CancellationToken cancellationToken);
    Task<OrganizerDirectorySettings> UpdateSettingsAsync(Guid organizerId, UpdateOrganizerDirectorySettings request, CancellationToken cancellationToken);
}

public sealed class OrganizerDirectoryService(AppDbContext dbContext, IImageLifecycleService imageLifecycleService, TimeProvider timeProvider) : IOrganizerDirectoryService
{
    public async Task<PaginatedResponse<PublicOrganizerSummary>> GetPublicAsync(string? search, string? category, int page, int pageSize, CancellationToken cancellationToken)
    {
        (page, pageSize) = Pagination.Normalize(page, pageSize);
        var query = dbContext.Users.AsNoTracking().Where(user => user.IsActive && user.Role != UserRole.Admin && user.IsOrganizerDirectoryVisible && user.OrganizedEvents.Any());
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(user => user.Name.ToLower().Contains(term));
        }
        if (!string.IsNullOrWhiteSpace(category))
        {
            var normalized = EventCategories.Normalize(category) ?? throw new ApiException(StatusCodes.Status400BadRequest, "Choose a supported event category.");
            query = query.Where(user => user.OrganizerSpecialties.Any(item => item.Category == normalized));
        }
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(user => user.Name).ThenBy(user => user.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(user => new PublicOrganizerSummary(user.Id, user.Name, user.ImageUrl,
                user.OrganizerBannerUrl, user.OrganizerBio,
                user.OrganizerSpecialties.OrderBy(item => item.Category).Select(item => item.Category).ToList(),
                user.VerificationStatus))
            .ToListAsync(cancellationToken);
        return new(items, page, pageSize, total, Pagination.TotalPages(total, pageSize));
    }

    public async Task<PublicOrganizerDetail> GetPublicByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var organizer = await dbContext.Users.AsNoTracking().Include(user => user.OrganizerSpecialties)
            .SingleOrDefaultAsync(user => user.Id == id && user.IsActive && user.Role != UserRole.Admin && user.IsOrganizerDirectoryVisible && user.OrganizedEvents.Any(), cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Organizer not found.");
        var events = await dbContext.Events.AsNoTracking().Include(item => item.Organizer)
            .Where(item => item.OrganizerId == id && item.IsPublished && item.Date <= timeProvider.GetUtcNow())
            .OrderByDescending(item => item.Date)
            .Select(item => new { Event = item, Registrations = item.Registrations.Count })
            .ToListAsync(cancellationToken);
        return new(organizer.Id, organizer.Name, organizer.ImageUrl, organizer.OrganizerBannerUrl,
            organizer.OrganizerBio, organizer.OrganizerInstagramUrl, organizer.OrganizerTwitterUrl,
            organizer.OrganizerFacebookUrl, organizer.OrganizerWebsiteUrl,
            organizer.OrganizerSpecialties.OrderBy(item => item.Category).Select(item => item.Category).ToList(),
            organizer.VerificationStatus,
            events.Select(item => item.Event.ToResponse(item.Registrations)).ToList());
    }

    public async Task<OrganizerDirectorySettings> GetSettingsAsync(Guid organizerId, CancellationToken cancellationToken)
    {
        var organizer = await dbContext.Users.AsNoTracking().Include(user => user.OrganizerSpecialties)
            .SingleOrDefaultAsync(user => user.Id == organizerId && user.Role != UserRole.Admin, cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Organizer account not found.");
        return ToSettings(organizer);
    }

    public async Task<OrganizerDirectorySettings> UpdateSettingsAsync(Guid organizerId, UpdateOrganizerDirectorySettings request, CancellationToken cancellationToken)
    {
        var organizer = await dbContext.Users.Include(user => user.OrganizerSpecialties)
            .SingleOrDefaultAsync(user => user.Id == organizerId && user.Role != UserRole.Admin, cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Organizer account not found.");
        if (request.IsVisible && !await dbContext.Events.AnyAsync(item => item.OrganizerId == organizerId, cancellationToken))
            throw new ApiException(StatusCodes.Status409Conflict, "Create an event before opting into the Organizer directory.");
        var specialties = request.Specialties.Distinct(StringComparer.OrdinalIgnoreCase).Select(value =>
            EventCategories.Normalize(value) ?? throw new ApiException(StatusCodes.Status400BadRequest, "Choose only supported event categories.")).ToList();
        var banner = await imageLifecycleService.ClaimAsync(organizerId, ImageUploadKind.OrganizerBanner,
            request.BannerUrl, organizer.OrganizerBannerUrl, organizer.OrganizerBannerObjectKey, cancellationToken);
        organizer.IsOrganizerDirectoryVisible = request.IsVisible;
        organizer.OrganizerBio = Normalize(request.Bio);
        organizer.OrganizerBannerUrl = banner.Url;
        organizer.OrganizerBannerObjectKey = banner.ObjectKey;
        organizer.OrganizerInstagramUrl = Normalize(request.InstagramUrl);
        organizer.OrganizerTwitterUrl = Normalize(request.TwitterUrl);
        organizer.OrganizerFacebookUrl = Normalize(request.FacebookUrl);
        organizer.OrganizerWebsiteUrl = Normalize(request.WebsiteUrl);
        organizer.OrganizerSpecialties.Clear();
        foreach (var specialty in specialties) organizer.OrganizerSpecialties.Add(new OrganizerSpecialty { OrganizerId = organizerId, Category = specialty });
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToSettings(organizer);
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static OrganizerDirectorySettings ToSettings(User user) => new(user.IsOrganizerDirectoryVisible,
        user.OrganizerBio, user.OrganizerBannerUrl, user.OrganizerInstagramUrl, user.OrganizerTwitterUrl,
        user.OrganizerFacebookUrl, user.OrganizerWebsiteUrl,
        user.OrganizerSpecialties.OrderBy(item => item.Category).Select(item => item.Category).ToList());
}
