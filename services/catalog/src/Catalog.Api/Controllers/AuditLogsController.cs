using Calendarun.Common.Auth;
using Calendarun.Common.Errors;
using Calendarun.Common.Http;
using Calendarun.Settings.Client;
using Catalog.Application.AuditLogs.Queries;
using Catalog.Application.Common;
using Calendarun.Common.Time;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Api.Controllers;

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
    /// Get audit logs (admin only)
    /// </summary>
    [HttpGet("/admin/audit")]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] string? entityType,
        [FromQuery] string? action,
        [FromQuery] Guid? actorUserId,
        [FromQuery] string? from,
        [FromQuery] string? to,
        [FromQuery] int page = 1,
        [FromQuery] int? pageSize = null,
        CancellationToken ct = default)
    {
        var isSuperAdmin = IsSuperAdmin();
        var role = GetTenantRole();

        if (!isSuperAdmin && role != nameof(TenantRole.TenantAdmin))
            return Forbid();

        var tenantId = GetTenantId();
        if (!isSuperAdmin && !tenantId.HasValue)
            return BadRequest(Calendarun.Common.Errors.ProblemDetailsFactory.Create(
                400,
                $"{HeaderNames.TenantId} header is required",
                HttpContext));

        // Get page size from Settings if not provided (tenant-specific or global)
        var tenantIdStr = tenantId?.ToString();
        var effectivePageSize = pageSize ?? await _settingsClient.GetAsync<int?>(
            AuditDefaults.SettingsKey, tenantIdStr, ct) ?? AuditDefaults.DefaultPageSize;

        if (!DateQueryParser.TryParseDateFilter(from, "from", false, out var dateFrom, out var fromError))
        {
            return BadRequest(Calendarun.Common.Errors.ProblemDetailsFactory.Create(400, fromError ?? "Invalid date format", HttpContext));
        }

        if (!DateQueryParser.TryParseDateFilter(to, "to", true, out var dateTo, out var toError))
        {
            return BadRequest(Calendarun.Common.Errors.ProblemDetailsFactory.Create(400, toError ?? "Invalid date format", HttpContext));
        }

        var query = new GetAuditLogsQuery(
            tenantId,
            isSuperAdmin,
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

    private Guid? GetTenantId()
    {
        var header = Request.Headers[HeaderNames.TenantId].FirstOrDefault();
        return Guid.TryParse(header, out var id) ? id : null;
    }

    private string? GetTenantRole()
    {
        return Request.Headers[HeaderNames.TenantRole].FirstOrDefault();
    }

    private bool IsSuperAdmin()
    {
        return Request.Headers[HeaderNames.IsSuperAdmin].FirstOrDefault() == "True";
    }

    private IActionResult ToActionResult<T>(Result<T> result, Func<T, IActionResult> onSuccess)
    {
        if (result.IsSuccess)
            return onSuccess(result.Value!);

        return result.ErrorType switch
        {
            ResultErrorType.NotFound => NotFound(Calendarun.Common.Errors.ProblemDetailsFactory.Create(404, result.Error ?? "Not found", HttpContext)),
            ResultErrorType.Forbidden => StatusCode(403, Calendarun.Common.Errors.ProblemDetailsFactory.Create(403, result.Error ?? "Access denied", HttpContext)),
            ResultErrorType.Conflict => Conflict(Calendarun.Common.Errors.ProblemDetailsFactory.Create(409, result.Error ?? "Conflict", HttpContext)),
            _ => BadRequest(Calendarun.Common.Errors.ProblemDetailsFactory.Create(400, result.Error ?? "Bad request", HttpContext))
        };
    }
}
