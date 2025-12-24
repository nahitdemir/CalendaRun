using Calendarun.Common.Auth;
using Calendarun.Settings.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Application.AuditLogs.Queries;
using Platform.Application.Common;
using System.Security.Claims;

namespace Platform.Api.Controllers;

[ApiController]
[Authorize]
public class AuditLogsController : ControllerBase
{
    private readonly IQueryHandler<GetAuditLogsQuery, Result<AuditLogPagedResult>> _getAuditLogsHandler;
    private readonly ISettingsClient _settingsClient;

    public AuditLogsController(
        IQueryHandler<GetAuditLogsQuery, Result<AuditLogPagedResult>> getAuditLogsHandler,
        ISettingsClient settingsClient)
    {
        _getAuditLogsHandler = getAuditLogsHandler;
        _settingsClient = settingsClient;
    }

    /// <summary>
    /// Get audit logs for super admin (all tenants)
    /// </summary>
    [HttpGet("/super-admin/audit")]
    public async Task<IActionResult> GetSuperAdminAuditLogs(
        [FromQuery] Guid? tenantId,
        [FromQuery] string? entityType,
        [FromQuery] string? action,
        [FromQuery] Guid? actorUserId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int page = 1,
        [FromQuery] int? pageSize = null,
        CancellationToken ct = default)
    {
        if (!IsSuperAdmin())
            return Forbid();

        // Get page size from Settings if not provided
        var effectivePageSize = pageSize ?? await _settingsClient.GetAsync<int?>(
            AuditDefaults.SettingsKey, null, ct) ?? AuditDefaults.DefaultPageSize;

        var query = new GetAuditLogsQuery(
            tenantId,
            IsSuperAdmin: true,
            entityType,
            action,
            actorUserId,
            from,
            to,
            page,
            effectivePageSize
        );

        var result = await _getAuditLogsHandler.HandleAsync(query, ct);
        return ToActionResult(result, Ok);
    }

    /// <summary>
    /// Get audit logs for tenant admin (own tenant only)
    /// </summary>
    [HttpGet("/admin/audit")]
    public async Task<IActionResult> GetAdminAuditLogs(
        [FromQuery] string? entityType,
        [FromQuery] string? action,
        [FromQuery] Guid? actorUserId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int page = 1,
        [FromQuery] int? pageSize = null,
        CancellationToken ct = default)
    {
        var tenantId = GetTenantIdFromHeader();
        var isSuperAdmin = IsSuperAdmin();
        var role = GetTenantRole();

        // Only tenant_admin or super_admin can access
        if (!isSuperAdmin && role != nameof(TenantRole.TenantAdmin))
            return Forbid();

        if (!isSuperAdmin && !tenantId.HasValue)
            return BadRequest(new { error = "X-Tenant-Id header is required" });

        // Get page size from Settings if not provided (tenant-specific or global)
        var tenantIdStr = tenantId?.ToString();
        var effectivePageSize = pageSize ?? await _settingsClient.GetAsync<int?>(
            AuditDefaults.SettingsKey, tenantIdStr, ct) ?? AuditDefaults.DefaultPageSize;

        var query = new GetAuditLogsQuery(
            tenantId,
            isSuperAdmin,
            entityType,
            action,
            actorUserId,
            from,
            to,
            page,
            effectivePageSize
        );

        var result = await _getAuditLogsHandler.HandleAsync(query, ct);
        return ToActionResult(result, Ok);
    }

    /// <summary>
    /// Get distinct actions for filtering
    /// </summary>
    [HttpGet("/admin/audit/actions")]
    public IActionResult GetActions()
    {
        var actions = new[]
        {
            "TenantCreated",
            "InviteCreated",
            "InviteAccepted",
            "MembershipRoleChanged",
            "EventCreated",
            "EventUpdated",
            "EventDeleted",
            "EventPublished",
            "MilestoneCreated",
            "MilestoneUpdated",
            "PlanCreated",
            "PlanDeleted",
            "PlanStateChanged"
        };
        return Ok(actions);
    }

    /// <summary>
    /// Get distinct entity types for filtering
    /// </summary>
    [HttpGet("/admin/audit/entity-types")]
    public IActionResult GetEntityTypes()
    {
        var entityTypes = new[]
        {
            "Tenant",
            "Membership",
            "Invite",
            "Event",
            "Milestone",
            "UserPlanItem"
        };
        return Ok(entityTypes);
    }

    private Guid GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }

    private Guid? GetTenantIdFromHeader()
    {
        var tenantIdHeader = Request.Headers["X-Tenant-Id"].FirstOrDefault();
        return Guid.TryParse(tenantIdHeader, out var tenantId) ? tenantId : null;
    }

    private string? GetTenantRole()
    {
        return Request.Headers["X-Tenant-Role"].FirstOrDefault();
    }

    private bool IsSuperAdmin()
    {
        return User.HasClaim(CalendarunClaimTypes.RealmRoles, Roles.SuperAdmin);
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

