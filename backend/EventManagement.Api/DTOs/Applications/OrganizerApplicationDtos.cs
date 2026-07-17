using System.ComponentModel.DataAnnotations;
using EventManagement.Api.Models;

namespace EventManagement.Api.DTOs.Applications;

public sealed record CreateOrganizerApplicationRequest(
    [param: Required, StringLength(2000, MinimumLength = 20)] string Reason);

public sealed record RejectOrganizerApplicationRequest(
    [param: StringLength(1000)] string? Reason);

public sealed record OrganizerApplicationResponse(
    Guid Id,
    Guid UserId,
    string UserName,
    string UserEmail,
    string Reason,
    ApplicationStatus Status,
    string? RejectionReason,
    DateTimeOffset SubmittedAt,
    DateTimeOffset? ReviewedAt,
    Guid? ReviewedByAdminId,
    string? ReviewedByAdminName);
