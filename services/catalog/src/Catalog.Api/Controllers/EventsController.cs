using Calendarun.Common.Auth;
using Catalog.Application.Common;
using Catalog.Application.Events.Commands;
using Catalog.Application.Events.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Api.Controllers;

[ApiController]
[Route("events")]
public class EventsController : ControllerBase
{
    private readonly IQueryHandler<GetEventsQuery, Result<List<EventDto>>> _getEventsHandler;
    private readonly IQueryHandler<GetEventByIdQuery, Result<EventDto>> _getEventByIdHandler;
    private readonly IQueryHandler<GetAdminEventsQuery, Result<List<AdminEventDto>>> _getAdminEventsHandler;
    private readonly ICommandHandler<CreateEventCommand, Result<CreateEventResult>> _createEventHandler;
    private readonly ICommandHandler<UpdateEventCommand, Result<UpdateEventResult>> _updateEventHandler;
    private readonly ICommandHandler<DeleteEventCommand, Result> _deleteEventHandler;

    public EventsController(
        IQueryHandler<GetEventsQuery, Result<List<EventDto>>> getEventsHandler,
        IQueryHandler<GetEventByIdQuery, Result<EventDto>> getEventByIdHandler,
        IQueryHandler<GetAdminEventsQuery, Result<List<AdminEventDto>>> getAdminEventsHandler,
        ICommandHandler<CreateEventCommand, Result<CreateEventResult>> createEventHandler,
        ICommandHandler<UpdateEventCommand, Result<UpdateEventResult>> updateEventHandler,
        ICommandHandler<DeleteEventCommand, Result> deleteEventHandler)
    {
        _getEventsHandler = getEventsHandler;
        _getEventByIdHandler = getEventByIdHandler;
        _getAdminEventsHandler = getAdminEventsHandler;
        _createEventHandler = createEventHandler;
        _updateEventHandler = updateEventHandler;
        _deleteEventHandler = deleteEventHandler;
    }

    /// <summary>
    /// Get events - supports tenant filtering
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetEvents(CancellationToken ct)
    {
        var tenantId = GetTenantId();
        var result = await _getEventsHandler.HandleAsync(new GetEventsQuery(tenantId), ct);
        return ToActionResult(result, Ok);
    }

    /// <summary>
    /// Get event by ID - public access (tenant filtering applied if X-Tenant-Id provided)
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetEventById(Guid id, CancellationToken ct)
    {
        var tenantId = GetTenantId();
        var result = await _getEventByIdHandler.HandleAsync(new GetEventByIdQuery(id, tenantId), ct);
        return ToActionResult(result, Ok);
    }

    /// <summary>
    /// Get distinct cities from events
    /// </summary>
    [HttpGet("cities")]
    public async Task<IActionResult> GetCities(CancellationToken ct)
    {
        var tenantId = GetTenantId();
        var result = await _getEventsHandler.HandleAsync(new GetEventsQuery(tenantId), ct);
        
        if (!result.IsSuccess)
            return ToActionResult(result, _ => Ok());

        var cities = result.Value!
            .Select(e => e.City)
            .Distinct()
            .OrderBy(c => c)
            .ToList();

        return Ok(cities);
    }

    /// <summary>
    /// Create event - requires authentication + tenant context
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateEvent([FromBody] CreateEventRequest request, CancellationToken ct)
    {
        var tenantId = GetTenantId();
        var userId = GetUserId();
        var isSuperAdmin = IsSuperAdmin();
        var role = GetTenantRole();

        if (!tenantId.HasValue)
            return BadRequest(new { error = "X-Tenant-Id header is required" });

        if (!userId.HasValue)
            return Unauthorized();

        // Only tenant_admin or super_admin can create events
        if (!isSuperAdmin && role != nameof(TenantRole.TenantAdmin))
            return Forbid();

        var command = new CreateEventCommand(
            tenantId.Value,
            userId.Value,
            GetUserEmail(),
            request.Title,
            request.Description,
            request.StartAt,
            request.City,
            request.CountryCode,
            request.RegistrationUrl,
            request.IsGlobal,
            isSuperAdmin,
            HttpContext.TraceIdentifier
        );

        var result = await _createEventHandler.HandleAsync(command, ct);
        return ToActionResult(result, e => Created($"/events/{e.Id}", e));
    }

    /// <summary>
    /// Update event
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> UpdateEvent(Guid id, [FromBody] UpdateEventRequest request, CancellationToken ct)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var command = new UpdateEventCommand(
            id,
            GetTenantId(),
            userId.Value,
            GetUserEmail(),
            IsSuperAdmin(),
            request.Title,
            request.Description,
            request.StartAt,
            request.City,
            request.CountryCode,
            request.RegistrationUrl,
            HttpContext.TraceIdentifier
        );

        var result = await _updateEventHandler.HandleAsync(command, ct);
        return ToActionResult(result, Ok);
    }

    /// <summary>
    /// Delete event (admin only)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteEvent(Guid id, CancellationToken ct)
    {
        var userId = GetUserId();
        var role = GetTenantRole();
        var isSuperAdmin = IsSuperAdmin();

        if (!userId.HasValue)
            return Unauthorized();

        // Only tenant_admin or super_admin can delete
        if (!isSuperAdmin && role != nameof(TenantRole.TenantAdmin))
            return Forbid();

        var command = new DeleteEventCommand(
            id,
            GetTenantId(),
            userId.Value,
            GetUserEmail(),
            isSuperAdmin,
            HttpContext.TraceIdentifier
        );

        var result = await _deleteEventHandler.HandleAsync(command, ct);
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

public record CreateEventRequest(
    string Title,
    string? Description,
    DateTimeOffset StartAt,
    string City,
    string CountryCode,
    string? RegistrationUrl,
    bool IsGlobal = false);

public record UpdateEventRequest(
    string? Title,
    string? Description,
    DateTimeOffset? StartAt,
    string? City,
    string? CountryCode,
    string? RegistrationUrl);

