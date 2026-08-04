using EventManagement.Api.Data;
using EventManagement.Api.DTOs.Auth;
using EventManagement.Api.DTOs.Common;
using EventManagement.Api.Infrastructure;
using EventManagement.Api.Mappings;
using EventManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Api.Services;

public interface IUserService
{
    Task<PaginatedResponse<UserResponse>> GetAsync(
        string? search,
        UserRole? role,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
    Task<UserResponse> UpdateRoleAsync(
        Guid userId,
        UserRole role,
        Guid adminId,
        CancellationToken cancellationToken);
    Task<UserResponse> UpdateProfileAsync(
        Guid userId,
        Guid actorId,
        string? imageUrl,
        CancellationToken cancellationToken);
    Task DeactivateAsync(Guid userId, Guid adminId, CancellationToken cancellationToken);
}

public sealed class UserService(
    AppDbContext dbContext,
    IImageLifecycleService imageLifecycleService) : IUserService
{
    public async Task<PaginatedResponse<UserResponse>> GetAsync(
        string? search,
        UserRole? role,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        (page, pageSize) = Pagination.Normalize(page, pageSize);
        var query = dbContext.Users.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(user =>
                user.Name.ToLower().Contains(term) || user.Email.ToLower().Contains(term));
        }
        if (role.HasValue) query = query.Where(user => user.Role == role.Value);
        if (isActive.HasValue) query = query.Where(user => user.IsActive == isActive.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var users = await query.OrderBy(user => user.Name)
            .ThenBy(user => user.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return new PaginatedResponse<UserResponse>(
            users.Select(user => user.ToResponse()).ToList(),
            page,
            pageSize,
            totalCount,
            Pagination.TotalPages(totalCount, pageSize));
    }

    public async Task<UserResponse> UpdateRoleAsync(
        Guid userId,
        UserRole role,
        Guid adminId,
        CancellationToken cancellationToken)
    {
        if (role is not (UserRole.Student or UserRole.Organizer))
            throw new ApiException(StatusCodes.Status400BadRequest, "This endpoint only promotes or demotes Organizer access.");
        var user = await dbContext.Users.SingleOrDefaultAsync(
            item => item.Id == userId,
            cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "User account not found.");
        if (user.Role == UserRole.Admin)
            throw new ApiException(StatusCodes.Status400BadRequest, "Admin roles cannot be changed through this endpoint.");

        user.Role = role;
        if (role == UserRole.Organizer)
        {
            var pendingApplications = await dbContext.OrganizerApplications
                .Where(application =>
                    application.UserId == userId && application.Status == ApplicationStatus.Pending)
                .ToListAsync(cancellationToken);
            foreach (var application in pendingApplications)
            {
                application.Status = ApplicationStatus.Approved;
                application.ReviewedAt = DateTimeOffset.UtcNow;
                application.ReviewedByAdminId = adminId;
                application.RejectionReason = null;
            }
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        return user.ToResponse();
    }

    public async Task<UserResponse> UpdateProfileAsync(
        Guid userId,
        Guid actorId,
        string? imageUrl,
        CancellationToken cancellationToken)
    {
        if (userId != actorId)
            throw new ApiException(StatusCodes.Status403Forbidden, "You may only update your own profile.");
        var user = await dbContext.Users.SingleOrDefaultAsync(
            item => item.Id == userId,
            cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "User account not found.");

        var image = await imageLifecycleService.ClaimAsync(
            actorId,
            ImageUploadKind.Profile,
            imageUrl,
            user.ImageUrl,
            user.ImageObjectKey,
            cancellationToken);
        user.ImageUrl = image.Url;
        user.ImageObjectKey = image.ObjectKey;
        await dbContext.SaveChangesAsync(cancellationToken);
        return user.ToResponse();
    }

    public async Task DeactivateAsync(
        Guid userId,
        Guid adminId,
        CancellationToken cancellationToken)
    {
        if (userId == adminId)
            throw new ApiException(StatusCodes.Status400BadRequest, "You cannot deactivate your own account.");
        var user = await dbContext.Users.SingleOrDefaultAsync(
            item => item.Id == userId,
            cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "User account not found.");
        if (!user.IsActive) return;
        user.IsActive = false;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
