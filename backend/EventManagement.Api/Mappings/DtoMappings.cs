using EventManagement.Api.DTOs.Applications;
using EventManagement.Api.DTOs.Auth;
using EventManagement.Api.DTOs.Events;
using EventManagement.Api.Models;

namespace EventManagement.Api.Mappings;

public static class DtoMappings
{
    public static UserResponse ToResponse(this User user) => new(
        user.Id,
        user.Name,
        user.Email,
        user.Role,
        user.IsActive,
        user.CreatedAt,
        user.ImageUrl);

    public static OrganizerApplicationResponse ToResponse(this OrganizerApplication application) => new(
        application.Id,
        application.UserId,
        application.User.Name,
        application.User.Email,
        application.Reason,
        application.Status,
        application.RejectionReason,
        application.SubmittedAt,
        application.ReviewedAt,
        application.ReviewedByAdminId,
        application.ReviewedByAdmin?.Name);

    public static EventResponse ToResponse(this EventEntity eventEntity, int registrationCount) => new(
        eventEntity.Id,
        eventEntity.Title,
        eventEntity.Description,
        eventEntity.Date,
        eventEntity.Location,
        eventEntity.Capacity,
        eventEntity.Category,
        eventEntity.OrganizerId,
        eventEntity.Organizer.Name,
        registrationCount,
        eventEntity.CreatedAt,
        eventEntity.ImageUrl,
        eventEntity.IsPublished,
        eventEntity.Version,
        eventEntity.PriceMinor,
        eventEntity.Currency);
}
