namespace EventManagement.Api.DTOs.Audit;

public sealed record AdminAuditLogResponse(
    Guid Id,
    Guid ActorUserId,
    string ActorName,
    string Action,
    string TargetType,
    string TargetId,
    string DetailsJson,
    DateTimeOffset CreatedAt);
