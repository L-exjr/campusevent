using System.Data;
using EventManagement.Api.Data;
using EventManagement.Api.DTOs.Applications;
using EventManagement.Api.DTOs.Common;
using EventManagement.Api.Infrastructure;
using EventManagement.Api.Mappings;
using EventManagement.Api.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EventManagement.Api.Services;

public interface IOrganizerApplicationService
{
    Task<OrganizerApplicationResponse> SubmitAsync(
        Guid userId,
        CreateOrganizerApplicationRequest request,
        CancellationToken cancellationToken);
    Task<OrganizerApplicationResponse?> GetLatestForUserAsync(
        Guid userId,
        CancellationToken cancellationToken);
    Task<PaginatedResponse<OrganizerApplicationResponse>> GetAsync(
        ApplicationStatus? status,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
    Task<OrganizerApplicationResponse> ApproveAsync(
        Guid applicationId,
        Guid adminId,
        CancellationToken cancellationToken);
    Task<OrganizerApplicationResponse> RejectAsync(
        Guid applicationId,
        Guid adminId,
        RejectOrganizerApplicationRequest request,
        CancellationToken cancellationToken);
}

public sealed class OrganizerApplicationService(
    AppDbContext dbContext,
    AdminAuditService auditService,
    TimeProvider timeProvider)
    : IOrganizerApplicationService
{
    public async Task<OrganizerApplicationResponse> SubmitAsync(
        Guid userId,
        CreateOrganizerApplicationRequest request,
        CancellationToken cancellationToken)
    {
        var reason = request.Reason.Trim();
        if (reason.Length < 20)
            throw new ApiException(
                StatusCodes.Status400BadRequest,
                "Tell us why your organizer identity should be verified using at least 20 characters.");

        var user = await dbContext.Users.SingleOrDefaultAsync(
            item => item.Id == userId,
            cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "User account not found.");
        if (user.Role == UserRole.Admin)
            throw new ApiException(StatusCodes.Status403Forbidden, "Admin accounts cannot request organizer verification.");
        if (user.VerificationStatus == VerificationStatus.Verified)
            throw new ApiException(StatusCodes.Status409Conflict, "Your organizer profile is already verified.");
        if (await dbContext.OrganizerApplications.AnyAsync(
            application => application.UserId == userId && application.Status == ApplicationStatus.Pending,
            cancellationToken))
        {
            throw new ApiException(StatusCodes.Status409Conflict, "You already have a pending organizer application.");
        }

        var application = new OrganizerApplication
        {
            UserId = userId,
            User = user,
            Reason = reason,
            Status = ApplicationStatus.Pending
        };
        dbContext.OrganizerApplications.Add(application);
        user.VerificationStatus = VerificationStatus.Pending;
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: "IX_OrganizerApplications_UserId"
            })
        {
            throw new ApiException(
                StatusCodes.Status409Conflict,
                "You already have a pending organizer application.");
        }
        return application.ToResponse();
    }

    public async Task<OrganizerApplicationResponse?> GetLatestForUserAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var application = await dbContext.OrganizerApplications.AsNoTracking()
            .Include(item => item.User)
            .Include(item => item.ReviewedByAdmin)
            .Where(item => item.UserId == userId)
            .OrderByDescending(item => item.SubmittedAt)
            .FirstOrDefaultAsync(cancellationToken);
        return application?.ToResponse();
    }

    public async Task<PaginatedResponse<OrganizerApplicationResponse>> GetAsync(
        ApplicationStatus? status,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        (page, pageSize) = Pagination.Normalize(page, pageSize);
        var query = dbContext.OrganizerApplications.AsNoTracking()
            .Include(application => application.User)
            .Include(application => application.ReviewedByAdmin)
            .AsQueryable();
        if (status.HasValue) query = query.Where(application => application.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(application =>
                application.User.Name.ToLower().Contains(term) ||
                application.User.Email.ToLower().Contains(term) ||
                application.Reason.ToLower().Contains(term));
        }
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(application => application.SubmittedAt)
            .ThenByDescending(application => application.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return new PaginatedResponse<OrganizerApplicationResponse>(
            items.Select(application => application.ToResponse()).ToList(),
            page,
            pageSize,
            totalCount,
            Pagination.TotalPages(totalCount, pageSize));
    }

    public Task<OrganizerApplicationResponse> ApproveAsync(
        Guid applicationId,
        Guid adminId,
        CancellationToken cancellationToken) =>
        ReviewAsync(applicationId, adminId, ApplicationStatus.Approved, null, cancellationToken);

    public Task<OrganizerApplicationResponse> RejectAsync(
        Guid applicationId,
        Guid adminId,
        RejectOrganizerApplicationRequest request,
        CancellationToken cancellationToken) =>
        ReviewAsync(
            applicationId,
            adminId,
            ApplicationStatus.Rejected,
            string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim(),
            cancellationToken);

    private async Task<OrganizerApplicationResponse> ReviewAsync(
        Guid applicationId,
        Guid adminId,
        ApplicationStatus status,
        string? rejectionReason,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        var application = await dbContext.OrganizerApplications
            .Include(item => item.User)
            .Include(item => item.ReviewedByAdmin)
            .SingleOrDefaultAsync(item => item.Id == applicationId, cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Organizer application not found.");
        if (application.Status != ApplicationStatus.Pending)
            throw new ApiException(StatusCodes.Status409Conflict, "This application has already been reviewed.");
        if (!application.User.IsActive)
            throw new ApiException(StatusCodes.Status409Conflict, "An inactive user cannot be verified.");
        if (application.User.Role == UserRole.Admin)
            throw new ApiException(StatusCodes.Status409Conflict, "Admin accounts cannot receive organizer verification.");

        application.Status = status;
        application.ReviewedAt = timeProvider.GetUtcNow();
        application.ReviewedByAdminId = adminId;
        application.RejectionReason = status == ApplicationStatus.Rejected ? rejectionReason : null;
        application.User.VerificationStatus = status == ApplicationStatus.Approved
            ? VerificationStatus.Verified
            : VerificationStatus.Unverified;
        EmailOutbox.EnqueueDomainMessage(
            dbContext,
            $"organizer-application-decision:{application.Id}",
            EmailOutbox.OrganizerApplicationDecisionKind,
            application.Id);
        auditService.Append(
            adminId,
            status == ApplicationStatus.Approved
                ? "OrganizerVerificationApproved"
                : "OrganizerVerificationRejected",
            "OrganizerVerificationRequest",
            application.Id,
            new { application.UserId, Status = status.ToString() });
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        application.ReviewedByAdmin = await dbContext.Users.AsNoTracking()
            .SingleAsync(user => user.Id == adminId, cancellationToken);

        return application.ToResponse();
    }
}
