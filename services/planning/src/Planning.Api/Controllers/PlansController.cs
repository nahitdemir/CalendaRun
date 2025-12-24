using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Planning.Application.Common;
using Planning.Application.Plans.Commands;
using Planning.Application.Plans.Queries;

namespace Planning.Api.Controllers;

[ApiController]
[Route("plan")]
public class PlansController : ControllerBase
{
    private readonly ICommandHandler<CreatePlanCommand, Result<CreatePlanResult>> _createPlanHandler;
    private readonly ICommandHandler<DeletePlanCommand, Result> _deletePlanHandler;
    private readonly IQueryHandler<GetUserPlansQuery, Result<List<PlanDto>>> _getUserPlansHandler;
    private readonly IQueryHandler<GetAdminPlansQuery, Result<List<AdminPlanDto>>> _getAdminPlansHandler;

    public PlansController(
        ICommandHandler<CreatePlanCommand, Result<CreatePlanResult>> createPlanHandler,
        ICommandHandler<DeletePlanCommand, Result> deletePlanHandler,
        IQueryHandler<GetUserPlansQuery, Result<List<PlanDto>>> getUserPlansHandler,
        IQueryHandler<GetAdminPlansQuery, Result<List<AdminPlanDto>>> getAdminPlansHandler)
    {
        _createPlanHandler = createPlanHandler;
        _deletePlanHandler = deletePlanHandler;
        _getUserPlansHandler = getUserPlansHandler;
        _getAdminPlansHandler = getAdminPlansHandler;
    }

    /// <summary>
    /// Create a new plan
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreatePlan([FromBody] CreatePlanRequest request, CancellationToken ct)
    {
        var tenantId = GetTenantId();
        var userId = GetUserId();
        var userEmail = GetUserEmail();

        if (!tenantId.HasValue || !userId.HasValue)
            return BadRequest(new { error = "X-Tenant-Id and X-User-Id headers are required" });

        var command = new CreatePlanCommand(
            tenantId.Value,
            userId.Value,
            userEmail ?? "unknown@local",
            request.EventId,
            HttpContext.TraceIdentifier
        );

        var result = await _createPlanHandler.HandleAsync(command, ct);
        return ToActionResult(result, r => Created($"/plan/{r.PlanItemId}", r));
    }

    /// <summary>
    /// Get user's plans
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetPlans(CancellationToken ct)
    {
        var tenantId = GetTenantId();
        var userId = GetUserId();

        if (!tenantId.HasValue || !userId.HasValue)
            return BadRequest(new { error = "X-Tenant-Id and X-User-Id headers are required" });

        var result = await _getUserPlansHandler.HandleAsync(
            new GetUserPlansQuery(tenantId.Value, userId.Value), ct);

        return ToActionResult(result, Ok);
    }

    /// <summary>
    /// Delete a plan (admin only)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> DeletePlan(Guid id, CancellationToken ct)
    {
        var userId = GetUserId();
        var role = GetTenantRole();
        var isSuperAdmin = IsSuperAdmin();

        if (!userId.HasValue)
            return Unauthorized();

        // Only tenant_admin or super_admin can delete
        if (!isSuperAdmin && role != "TenantAdmin")
            return Forbid();

        var command = new DeletePlanCommand(
            id,
            GetTenantId(),
            userId.Value,
            GetUserEmail(),
            isSuperAdmin,
            HttpContext.TraceIdentifier
        );

        var result = await _deletePlanHandler.HandleAsync(command, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    private Guid? GetTenantId()
    {
        var header = Request.Headers["X-Tenant-Id"].FirstOrDefault();
        return Guid.TryParse(header, out var id) ? id : null;
    }

    private Guid? GetUserId()
    {
        var header = Request.Headers["X-User-Id"].FirstOrDefault();
        return Guid.TryParse(header, out var id) ? id : null;
    }

    private string? GetUserEmail()
    {
        return Request.Headers["X-User-Email"].FirstOrDefault();
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

    private IActionResult ToActionResult(Result result)
    {
        if (result.IsSuccess)
            return NoContent();

        return result.ErrorType switch
        {
            ResultErrorType.NotFound => NotFound(new { error = result.Error }),
            ResultErrorType.Forbidden => Forbid(),
            ResultErrorType.Conflict => Conflict(new { error = result.Error }),
            _ => BadRequest(new { error = result.Error })
        };
    }
}

public record CreatePlanRequest(Guid EventId);

