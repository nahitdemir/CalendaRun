using Calendarun.Common.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Application.Common;
using Platform.Application.Tenants.Commands;
using Platform.Application.Tenants.Queries;
using System.Security.Claims;

namespace Platform.Api.Controllers;

[ApiController]
[Authorize]
public class TenantsController : ControllerBase
{
    private readonly ICommandHandler<CreateTenantCommand, Result<CreateTenantResult>> _createTenantHandler;
    private readonly IQueryHandler<GetAllTenantsQuery, Result<List<TenantDto>>> _getAllTenantsHandler;
    private readonly IQueryHandler<GetUserTenantsQuery, Result<List<UserTenantDto>>> _getUserTenantsHandler;

    public TenantsController(
        ICommandHandler<CreateTenantCommand, Result<CreateTenantResult>> createTenantHandler,
        IQueryHandler<GetAllTenantsQuery, Result<List<TenantDto>>> getAllTenantsHandler,
        IQueryHandler<GetUserTenantsQuery, Result<List<UserTenantDto>>> getUserTenantsHandler)
    {
        _createTenantHandler = createTenantHandler;
        _getAllTenantsHandler = getAllTenantsHandler;
        _getUserTenantsHandler = getUserTenantsHandler;
    }

    /// <summary>
    /// Create a new tenant (super_admin only)
    /// </summary>
    [HttpPost("/super-admin/tenants")]
    public async Task<IActionResult> Create([FromBody] CreateTenantRequest request, CancellationToken ct)
    {
        if (!IsSuperAdmin())
            return Forbid();

        var command = new CreateTenantCommand(
            request.Name,
            request.Slug,
            request.Description,
            request.DefaultLanguage,
            request.DefaultCurrency,
            GetUserId(),
            GetUserEmail()
        );

        var result = await _createTenantHandler.HandleAsync(command, ct);

        return ToActionResult(result, tenant => Created($"/super-admin/tenants/{tenant.Id}", tenant));
    }

    /// <summary>
    /// List all tenants (super_admin only)
    /// </summary>
    [HttpGet("/super-admin/tenants")]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        if (!IsSuperAdmin())
            return Forbid();

        var result = await _getAllTenantsHandler.HandleAsync(new GetAllTenantsQuery(), ct);

        return ToActionResult(result, Ok);
    }

    /// <summary>
    /// List tenants for current user
    /// </summary>
    [HttpGet("/me/tenants")]
    public async Task<IActionResult> GetMyTenants(CancellationToken ct)
    {
        var result = await _getUserTenantsHandler.HandleAsync(new GetUserTenantsQuery(GetUserId()), ct);

        return ToActionResult(result, Ok);
    }

    private Guid GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }

    private string? GetUserEmail()
    {
        return User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email");
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

public record CreateTenantRequest(
    string Name,
    string? Slug,
    string? Description,
    string? DefaultLanguage,
    string? DefaultCurrency
);

