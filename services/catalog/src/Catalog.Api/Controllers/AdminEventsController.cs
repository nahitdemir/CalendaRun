using Calendarun.Common.Auth;
using Catalog.Application.Common;
using Catalog.Application.Events.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Api.Controllers;

[ApiController]
[Route("admin/events")]
[Authorize]
public class AdminEventsController : ControllerBase
{
    private readonly IQueryHandler<GetAdminEventsQuery, Result<List<AdminEventDto>>> _getAdminEventsHandler;

    public AdminEventsController(
        IQueryHandler<GetAdminEventsQuery, Result<List<AdminEventDto>>> getAdminEventsHandler)
    {
        _getAdminEventsHandler = getAdminEventsHandler;
    }

    /// <summary>
    /// List events for admin (tenant-scoped or all for super_admin)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAdminEvents(CancellationToken ct)
    {
        var isSuperAdmin = IsSuperAdmin();
        var role = GetTenantRole();

        // Authorization check
        if (!isSuperAdmin && role != nameof(TenantRole.TenantAdmin))
            return Forbid();

        var query = new GetAdminEventsQuery(GetTenantId(), isSuperAdmin);
        var result = await _getAdminEventsHandler.HandleAsync(query, ct);

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

