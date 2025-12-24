using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Planning.Application.AuditLogs.Queries;
using Planning.Application.Common;
using Planning.Application.Plans.Queries;
using Planning.Application.Users.Queries;

namespace Planning.Api.Controllers;

[ApiController]
[Route("admin")]
[Authorize]
public class AdminController : ControllerBase
{
    private readonly IQueryHandler<GetAdminPlansQuery, Result<List<AdminPlanDto>>> _getAdminPlansHandler;
    private readonly IQueryHandler<GetAdminUsersQuery, Result<List<AdminUserDto>>> _getAdminUsersHandler;
    private readonly IQueryHandler<GetAuditLogsQuery, Result<AuditLogPagedResult>> _getAuditLogsHandler;

    public AdminController(
        IQueryHandler<GetAdminPlansQuery, Result<List<AdminPlanDto>>> getAdminPlansHandler,
        IQueryHandler<GetAdminUsersQuery, Result<List<AdminUserDto>>> getAdminUsersHandler,
        IQueryHandler<GetAuditLogsQuery, Result<AuditLogPagedResult>> getAuditLogsHandler)
    {
        _getAdminPlansHandler = getAdminPlansHandler;
        _getAdminUsersHandler = getAdminUsersHandler;
        _getAuditLogsHandler = getAuditLogsHandler;
    }

    /// <summary>
    /// List all plans for admin (tenant-scoped or all for super_admin)
    /// </summary>
    [HttpGet("plans")]
    public async Task<IActionResult> GetAdminPlans(CancellationToken ct)
    {
        var isSuperAdmin = IsSuperAdmin();
        var role = GetTenantRole();

        // Authorization check - only tenant_admin or super_admin
        if (!isSuperAdmin && role != "TenantAdmin")
            return Forbid();

        var result = await _getAdminPlansHandler.HandleAsync(
            new GetAdminPlansQuery(GetTenantId(), isSuperAdmin), ct);

        return ToActionResult(result, Ok);
    }

    /// <summary>
    /// List users in tenant (tenant_admin or super_admin)
    /// </summary>
    [HttpGet("users")]
    public async Task<IActionResult> GetAdminUsers(CancellationToken ct)
    {
        var isSuperAdmin = IsSuperAdmin();
        var role = GetTenantRole();

        if (!isSuperAdmin && role != "TenantAdmin")
            return Forbid();

        var result = await _getAdminUsersHandler.HandleAsync(
            new GetAdminUsersQuery(GetTenantId(), isSuperAdmin), ct);

        return ToActionResult(result, Ok);
    }

    /// <summary>
    /// Get audit logs (admin only)
    /// </summary>
    [HttpGet("audit")]
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

