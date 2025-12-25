using Calendarun.Common.Auth;
using Calendarun.Common.Errors;
using Calendarun.Common.Http;
using Calendarun.Common.Time;
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
        [FromQuery] string? from,
        [FromQuery] string? to,
        [FromQuery] int page = 1,
        [FromQuery] int? pageSize = null,
        CancellationToken ct = default)
    {
        if (!IsSuperAdmin())
            return Forbid();

        // Get page size from Settings if not provided
        var effectivePageSize = pageSize ?? await _settingsClient.GetAsync<int?>(
            AuditDefaults.SettingsKey, null, ct) ?? AuditDefaults.DefaultPageSize;

        if (!DateQueryParser.TryParseDateFilter(from, "from", false, out var dateFrom, out var fromError))
        {
            return BadRequest(ProblemDetailsFactory.Create(400, fromError ?? "Invalid date format", HttpContext));
        }

        if (!DateQueryParser.TryParseDateFilter(to, "to", true, out var dateTo, out var toError))
        {
            return BadRequest(ProblemDetailsFactory.Create(400, toError ?? "Invalid date format", HttpContext));
        }

        var query = new GetAuditLogsQuery(
            tenantId,
            IsSuperAdmin: true,
            entityType,
            action,
            actorUserId,
            dateFrom,
            dateTo,
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
        [FromQuery] string? from,
        [FromQuery] string? to,
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
            return BadRequest(ProblemDetailsFactory.Create(
                400,
                $"{HeaderNames.TenantId} header is required",
                HttpContext));

        // Get page size from Settings if not provided (tenant-specific or global)
        var tenantIdStr = tenantId?.ToString();
        var effectivePageSize = pageSize ?? await _settingsClient.GetAsync<int?>(
            AuditDefaults.SettingsKey, tenantIdStr, ct) ?? AuditDefaults.DefaultPageSize;

        if (!DateQueryParser.TryParseDateFilter(from, "from", false, out var adminDateFrom, out var adminFromError))
        {
            return BadRequest(ProblemDetailsFactory.Create(400, adminFromError ?? "Invalid date format", HttpContext));
        }

        if (!DateQueryParser.TryParseDateFilter(to, "to", true, out var adminDateTo, out var adminToError))
        {
            return BadRequest(ProblemDetailsFactory.Create(400, adminToError ?? "Invalid date format", HttpContext));
        }

        var query = new GetAuditLogsQuery(
            tenantId,
            isSuperAdmin,
            entityType,
            action,
            actorUserId,
            adminDateFrom,
            adminDateTo,
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
        var tenantIdHeader = Request.Headers[HeaderNames.TenantId].FirstOrDefault();
        return Guid.TryParse(tenantIdHeader, out var tenantId) ? tenantId : null;
    }

    private string? GetTenantRole()
    {
        return Request.Headers[HeaderNames.TenantRole].FirstOrDefault();
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
            ResultErrorType.NotFound => NotFound(ProblemDetailsFactory.Create(404, result.Error ?? "Not found", HttpContext)),
            ResultErrorType.Forbidden => StatusCode(403, ProblemDetailsFactory.Create(403, result.Error ?? "Access denied", HttpContext)),
            ResultErrorType.Conflict => Conflict(ProblemDetailsFactory.Create(409, result.Error ?? "Conflict", HttpContext)),
            _ => BadRequest(ProblemDetailsFactory.Create(400, result.Error ?? "Bad request", HttpContext))
        };
    }
}
