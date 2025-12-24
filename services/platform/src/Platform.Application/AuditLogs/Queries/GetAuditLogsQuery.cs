using Platform.Application.Common;

namespace Platform.Application.AuditLogs.Queries;

public record GetAuditLogsQuery(
    Guid? TenantId,
    bool IsSuperAdmin,
    string? EntityType,
    string? Action,
    Guid? ActorUserId,
    DateTimeOffset? From,
    DateTimeOffset? To,
    int Page = 1,
    int PageSize = 50
) : IQuery<Result<AuditLogPagedResult>>;

public record AuditLogPagedResult(
    List<AuditLogDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);

public record AuditLogDto(
    Guid Id,
    Guid? TenantId,
    Guid ActorUserId,
    string? ActorEmail,
    string Action,
    string EntityType,
    Guid? EntityId,
    string? BeforeJson,
    string? AfterJson,
    string? TraceId,
    DateTimeOffset Timestamp
);

