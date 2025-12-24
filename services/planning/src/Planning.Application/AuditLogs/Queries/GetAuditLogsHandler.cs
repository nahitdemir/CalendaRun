using Microsoft.EntityFrameworkCore;
using Planning.Application.Common;
using Planning.Infrastructure;

namespace Planning.Application.AuditLogs.Queries;

public class GetAuditLogsHandler : IQueryHandler<GetAuditLogsQuery, Result<AuditLogPagedResult>>
{
    private readonly PlanningDbContext _db;

    public GetAuditLogsHandler(PlanningDbContext db)
    {
        _db = db;
    }

    public async Task<Result<AuditLogPagedResult>> HandleAsync(GetAuditLogsQuery query, CancellationToken ct = default)
    {
        var logsQuery = _db.AuditLogs.AsQueryable();

        // Tenant filtering
        if (!query.IsSuperAdmin)
        {
            if (!query.TenantId.HasValue)
                return Result<AuditLogPagedResult>.Failure("Tenant ID is required");

            logsQuery = logsQuery.Where(a => a.TenantId == query.TenantId);
        }
        else if (query.TenantId.HasValue)
        {
            logsQuery = logsQuery.Where(a => a.TenantId == query.TenantId);
        }

        // Additional filters
        if (!string.IsNullOrEmpty(query.EntityType))
            logsQuery = logsQuery.Where(a => a.EntityType == query.EntityType);

        if (!string.IsNullOrEmpty(query.Action))
            logsQuery = logsQuery.Where(a => a.Action == query.Action);

        if (query.ActorUserId.HasValue)
            logsQuery = logsQuery.Where(a => a.ActorUserId == query.ActorUserId);

        if (query.From.HasValue)
            logsQuery = logsQuery.Where(a => a.Timestamp >= query.From.Value);

        if (query.To.HasValue)
            logsQuery = logsQuery.Where(a => a.Timestamp <= query.To.Value);

        var totalCount = await logsQuery.CountAsync(ct);

        var items = await logsQuery
            .OrderByDescending(a => a.Timestamp)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(a => new AuditLogDto(
                a.Id,
                a.TenantId,
                a.ActorUserId,
                a.ActorEmail,
                a.Action,
                a.EntityType,
                a.EntityId,
                a.BeforeJson,
                a.AfterJson,
                a.TraceId,
                a.Timestamp
            ))
            .ToListAsync(ct);

        var totalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize);

        return Result<AuditLogPagedResult>.Success(new AuditLogPagedResult(
            items,
            totalCount,
            query.Page,
            query.PageSize,
            totalPages
        ));
    }
}

