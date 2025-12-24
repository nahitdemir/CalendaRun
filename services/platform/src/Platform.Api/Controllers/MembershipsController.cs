using Calendarun.Common.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Application.Common;
using Platform.Application.Memberships.Queries;
using Platform.Domain.Entities;
using Platform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Platform.Api.Controllers;

[ApiController]
[Authorize]
public class MembershipsController : ControllerBase
{
    private readonly IQueryHandler<GetTenantUsersQuery, Result<List<TenantUserDto>>> _getTenantUsersHandler;
    private readonly IQueryHandler<ValidateMembershipQuery, MembershipValidationResult> _validateMembershipHandler;
    private readonly PlatformDbContext _db;

    public MembershipsController(
        IQueryHandler<GetTenantUsersQuery, Result<List<TenantUserDto>>> getTenantUsersHandler,
        IQueryHandler<ValidateMembershipQuery, MembershipValidationResult> validateMembershipHandler,
        PlatformDbContext db)
    {
        _getTenantUsersHandler = getTenantUsersHandler;
        _validateMembershipHandler = validateMembershipHandler;
        _db = db;
    }

    /// <summary>
    /// List users in current tenant (tenant_admin only)
    /// </summary>
    [HttpGet("/admin/users")]
    public async Task<IActionResult> GetTenantUsers(CancellationToken ct)
    {
        var tenantId = GetTenantIdFromHeader();
        if (tenantId == null)
            return BadRequest(new { error = "X-Tenant-Id header is required" });

        // Check authorization
        if (!IsSuperAdmin())
        {
            var membership = await _db.Memberships
                .FirstOrDefaultAsync(m => m.UserId == GetUserId() && 
                                          m.TenantId == tenantId && 
                                          m.Status == MembershipStatus.Active, ct);

            if (membership == null || membership.Role != TenantRole.TenantAdmin)
                return Forbid();
        }

        var result = await _getTenantUsersHandler.HandleAsync(new GetTenantUsersQuery(tenantId.Value), ct);

        return ToActionResult(result, Ok);
    }

    /// <summary>
    /// Validate user membership in tenant (for Gateway)
    /// </summary>
    [HttpGet("/api/memberships/validate")]
    [AllowAnonymous]
    public async Task<IActionResult> Validate([FromQuery] Guid userId, [FromQuery] Guid tenantId, CancellationToken ct)
    {
        var result = await _validateMembershipHandler.HandleAsync(
            new ValidateMembershipQuery(userId, tenantId), ct);

        return Ok(result);
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

