using System.Text.Json;
using EventManagement.Api.Data;
using EventManagement.Api.DTOs.Audit;
using EventManagement.Api.DTOs.Common;
using EventManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Api.Services;

public sealed class AdminAuditService(AppDbContext dbContext)
{
    public void Append(
        Guid actorUserId,
        string action,
        string targetType,
        object targetId,
        object details)
    {
        dbContext.AdminAuditLogs.Add(new AdminAuditLog
        {
            ActorUserId = actorUserId,
            Action = action,
            TargetType = targetType,
            TargetId = targetId.ToString() ?? string.Empty,
            DetailsJson = JsonSerializer.Serialize(details)
        });
    }

    public async Task<PaginatedResponse<AdminAuditLogResponse>> GetAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        (page, pageSize) = Pagination.Normalize(page, pageSize);
        var query = dbContext.AdminAuditLogs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(log =>
                log.Action.ToLower().Contains(term) ||
                log.TargetType.ToLower().Contains(term) ||
                log.TargetId.ToLower().Contains(term) ||
                log.ActorUser.Name.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var logs = await query
            .OrderByDescending(log => log.CreatedAt)
            .ThenByDescending(log => log.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(log => new AdminAuditLogResponse(
                log.Id,
                log.ActorUserId,
                log.ActorUser.Name,
                log.Action,
                log.TargetType,
                log.TargetId,
                log.DetailsJson,
                log.CreatedAt))
            .ToListAsync(cancellationToken);
        return new PaginatedResponse<AdminAuditLogResponse>(
            logs,
            page,
            pageSize,
            totalCount,
            Pagination.TotalPages(totalCount, pageSize));
    }
}
