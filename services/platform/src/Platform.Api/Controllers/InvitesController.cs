using Calendarun.Common.Auth;
using Calendarun.Common.Errors;
using Calendarun.Common.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Application.Common;
using Platform.Application.Invites.Commands;
using Platform.Domain.Entities;
using Platform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Platform.Api.Controllers;

[ApiController]
[Authorize]
public class InvitesController : ControllerBase
{
    private readonly ICommandHandler<CreateInviteCommand, Result<CreateInviteResult>> _createInviteHandler;
    private readonly ICommandHandler<AcceptInviteCommand, Result<AcceptInviteResult>> _acceptInviteHandler;
    private readonly PlatformDbContext _db;

    public InvitesController(
        ICommandHandler<CreateInviteCommand, Result<CreateInviteResult>> createInviteHandler,
        ICommandHandler<AcceptInviteCommand, Result<AcceptInviteResult>> acceptInviteHandler,
        PlatformDbContext db)
    {
        _createInviteHandler = createInviteHandler;
        _acceptInviteHandler = acceptInviteHandler;
        _db = db;
    }

    /// <summary>
    /// Create invite in current tenant (tenant_admin only)
    /// </summary>
    [HttpPost("/admin/invites")]
    public async Task<IActionResult> Create([FromBody] CreateInviteRequest request, CancellationToken ct)
    {
        var tenantId = GetTenantIdFromHeader();
        if (tenantId == null)
            return BadRequest(Calendarun.Common.Errors.ProblemDetailsFactory.Create(
                400,
                $"{HeaderNames.TenantId} header is required",
                HttpContext));

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

        var command = new CreateInviteCommand(
            tenantId.Value,
            request.Email,
            request.Role,
            GetUserId(),
            GetUserEmail()
        );

        var result = await _createInviteHandler.HandleAsync(command, ct);

        return ToActionResult(result, invite => Created($"/admin/invites/{invite.Id}", invite));
    }

    /// <summary>
    /// Accept invite (creates membership for user)
    /// </summary>
    [HttpPost("/invites/accept")]
    public async Task<IActionResult> Accept([FromBody] AcceptInviteRequest request, CancellationToken ct)
    {
        var command = new AcceptInviteCommand(
            request.Token,
            GetUserId(),
            GetUserEmail()
        );

        var result = await _acceptInviteHandler.HandleAsync(command, ct);

        return ToActionResult(result, Ok);
    }

    private Guid GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }

    private string GetUserEmail()
    {
        return User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email") ?? "";
    }

    private Guid? GetTenantIdFromHeader()
    {
        var tenantIdHeader = Request.Headers[HeaderNames.TenantId].FirstOrDefault();
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
            ResultErrorType.NotFound => NotFound(Calendarun.Common.Errors.ProblemDetailsFactory.Create(404, result.Error ?? "Not found", HttpContext)),
            ResultErrorType.Forbidden => StatusCode(403, Calendarun.Common.Errors.ProblemDetailsFactory.Create(403, result.Error ?? "Access denied", HttpContext)),
            ResultErrorType.Conflict => Conflict(Calendarun.Common.Errors.ProblemDetailsFactory.Create(409, result.Error ?? "Conflict", HttpContext)),
            _ => BadRequest(Calendarun.Common.Errors.ProblemDetailsFactory.Create(400, result.Error ?? "Bad request", HttpContext))
        };
    }
}

public record CreateInviteRequest(string Email, TenantRole Role);
public record AcceptInviteRequest(string Token);
