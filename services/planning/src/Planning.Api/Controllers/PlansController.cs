using Calendarun.Common.Auth;
using Calendarun.Common.Errors;
using Calendarun.Common.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Planning.Application.Common;
using Planning.Application.Plans.Commands;
using Planning.Application.Plans.Queries;
using Planning.Domain;

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
        var tenantId = GetTenantId(); // Optional - can be null for public events
        var userId = GetUserId();
        var userEmail = GetUserEmail();

        if (!userId.HasValue)
            return BadRequest(Calendarun.Common.Errors.ProblemDetailsFactory.Create(
                400,
                $"{HeaderNames.UserId} header is required",
                HttpContext));

        var command = new CreatePlanCommand(
            tenantId, // Now optional
            userId.Value,
            userEmail ?? PlanningDefaults.UnknownUserEmail,
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
        var tenantId = GetTenantId(); // Optional filter
        var userId = GetUserId();

        if (!userId.HasValue)
            return BadRequest(Calendarun.Common.Errors.ProblemDetailsFactory.Create(
                400,
                $"{HeaderNames.UserId} header is required",
                HttpContext));

        var result = await _getUserPlansHandler.HandleAsync(
            new GetUserPlansQuery(tenantId, userId.Value), ct);

        return ToActionResult(result, Ok);
    }

    /// <summary>
    /// Delete a plan
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> DeletePlan(Guid id, CancellationToken ct)
    {
        var userId = GetUserId();
        var role = GetTenantRole();
        var isSuperAdmin = IsSuperAdmin();
        var isTenantAdmin = role == nameof(TenantRole.TenantAdmin);

        if (!userId.HasValue)
            return Unauthorized();

        var command = new DeletePlanCommand(
            id,
            GetTenantId(),
            userId.Value,
            GetUserEmail(),
            isSuperAdmin,
            isTenantAdmin,
            HttpContext.TraceIdentifier
        );

        var result = await _deletePlanHandler.HandleAsync(command, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    private Guid? GetTenantId()
    {
        var header = Request.Headers[HeaderNames.TenantId].FirstOrDefault();
        return Guid.TryParse(header, out var id) ? id : null;
    }

    private Guid? GetUserId()
    {
        var header = Request.Headers[HeaderNames.UserId].FirstOrDefault();
        return Guid.TryParse(header, out var id) ? id : null;
    }

    private string? GetUserEmail()
    {
        return Request.Headers[HeaderNames.UserEmail].FirstOrDefault();
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

    private IActionResult ToActionResult(Result result)
    {
        if (result.IsSuccess)
            return NoContent();

        return result.ErrorType switch
        {
            ResultErrorType.NotFound => NotFound(Calendarun.Common.Errors.ProblemDetailsFactory.Create(404, result.Error ?? "Not found", HttpContext)),
            ResultErrorType.Forbidden => StatusCode(403, Calendarun.Common.Errors.ProblemDetailsFactory.Create(403, result.Error ?? "Access denied", HttpContext)),
            ResultErrorType.Conflict => Conflict(Calendarun.Common.Errors.ProblemDetailsFactory.Create(409, result.Error ?? "Conflict", HttpContext)),
            _ => BadRequest(Calendarun.Common.Errors.ProblemDetailsFactory.Create(400, result.Error ?? "Bad request", HttpContext))
        };
    }
}

public record CreatePlanRequest(Guid EventId);
