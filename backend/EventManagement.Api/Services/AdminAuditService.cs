using System.Text.Json;
using System.Text;
using EventManagement.Api.Data;
using EventManagement.Api.DTOs.Audit;
using EventManagement.Api.DTOs.Common;
using EventManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Api.Services;

public sealed class AdminAuditService(AppDbContext dbContext, TimeProvider timeProvider)
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
            DetailsJson = JsonSerializer.Serialize(details),
            CreatedAt = timeProvider.GetUtcNow()
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

    public async Task<byte[]> ExportCsvAsync(
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken cancellationToken)
    {
        var end = to ?? timeProvider.GetUtcNow();
        var start = from ?? end.AddDays(-30);
        if (start > end)
            throw new Infrastructure.ApiException(StatusCodes.Status400BadRequest, "The export start must be before its end.");
        if (end - start > TimeSpan.FromDays(366))
            throw new Infrastructure.ApiException(StatusCodes.Status400BadRequest, "Audit exports are limited to a 366-day range.");

        var rows = await dbContext.AdminAuditLogs.AsNoTracking()
            .Where(log => log.CreatedAt >= start && log.CreatedAt <= end)
            .OrderByDescending(log => log.CreatedAt)
            .ThenByDescending(log => log.Id)
            .Take(50_000)
            .Select(log => new
            {
                log.CreatedAt,
                log.ActorUserId,
                ActorName = log.ActorUser.Name,
                log.Action,
                log.TargetType,
                log.TargetId,
                log.DetailsJson
            })
            .ToListAsync(cancellationToken);
        var csv = new StringBuilder("createdAt,actorUserId,actorName,action,targetType,targetId,detailsJson\r\n");
        foreach (var row in rows)
        {
            csv.Append(Csv(row.CreatedAt.ToString("O"))).Append(',')
                .Append(Csv(row.ActorUserId.ToString())).Append(',')
                .Append(Csv(row.ActorName)).Append(',')
                .Append(Csv(row.Action)).Append(',')
                .Append(Csv(row.TargetType)).Append(',')
                .Append(Csv(row.TargetId)).Append(',')
                .Append(Csv(row.DetailsJson)).Append("\r\n");
        }
        return Encoding.UTF8.GetBytes(csv.ToString());
    }

    private static string Csv(string value)
    {
        // Quoting alone does not stop spreadsheet applications from evaluating
        // user-controlled names or text as formulas when an administrator opens the export.
        var safeValue = value.Length > 0 && "=+-@\t\r".Contains(value[0])
            ? $"'{value}"
            : value;
        return $"\"{safeValue.Replace("\"", "\"\"")}\"";
    }
}
