using Catalog.Application.AuditLogs.Queries;
using Catalog.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Api.Controllers;

[ApiController]
[Authorize]
public class AuditLogsController : ControllerBase
{
    private readonly IQueryHandler<GetAuditLogsQuery, Result<AuditLogPagedResult>> _getAuditLogsHandler;

    public AuditLogsController(
        IQueryHandler<GetAuditLogsQuery, Result<AuditLogPagedResult>> getAuditLogsHandler)
    {
        _getAuditLogsHandler = getAuditLogsHandler;
    }

    /// <summary>
    /// Get audit logs (admin only)
    /// </summary>
    [HttpGet("/admin/audit")]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] string? entityType,
        [FromQuery] string? action,
        [FromQuery] Guid? actorUserId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var isSuperAdmin = IsSuperAdmin();
        var role = GetTenantRole();

        if (!isSuperAdmin && role != "TenantAdmin")
            return Forbid();

        var tenantId = GetTenantId();
        if (!isSuperAdmin && !tenantId.HasValue)
            return BadRequest(new { error = "X-Tenant-Id header is required" });

        var query = new GetAuditLogsQuery(
            tenantId,
            isSuperAdmin,
            entityType,
            action,
            actorUserId,
            from,
            to,
            page,
            pageSize
        );

        var result = await _getAuditLogsHandler.HandleAsync(query, ct);
        return ToActionResult(result, Ok);
    }

    private Guid? GetTenantId()
    {
        var header = Request.Headers["X-Tenant-Id"].FirstOrDefault();
        return Guid.TryParse(header, out var id) ? id : null;
    }

    private string? GetTenantRole()
    {
        return Request.Headers["X-Tenant-Role"].FirstOrDefault();
    }

    private bool IsSuperAdmin()
    {
        return Request.Headers["X-Is-Super-Admin"].FirstOrDefault() == "True";
    }

    private IActionResult ToActionResult<T>(Result<T> result, Func<T, IActionResult> onSuccess)
    {
        if (result.IsSuccess)
            return onSuccess(result.Value!);

        return result.ErrorType switch
        {
            ResultErrorType.NotFound => NotFound(new { error = result.Error }),
            ResultErrorType.Forbidden => Forbid(),
            ResultErrorType.Conflict => Conflict(new { error = result.Error }),
            _ => BadRequest(new { error = result.Error })
        };
    }
}

