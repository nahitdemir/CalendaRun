using Calendarun.Common.Auth;
using Calendarun.Common.Errors;
using Calendarun.Common.Http;
using Catalog.Application.Common;
using Calendarun.Common.Time;
using Catalog.Application.Events.Commands;
using Catalog.Application.Events.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace Catalog.Api.Controllers;

[ApiController]
[Route("events")]
public class EventsController : ControllerBase
{
    private readonly IQueryHandler<GetEventsQuery, Result<EventsPagedResult>> _getEventsHandler;
    private readonly IQueryHandler<GetEventByIdQuery, Result<EventDto>> _getEventByIdHandler;
    private readonly IQueryHandler<GetAdminEventsQuery, Result<List<AdminEventDto>>> _getAdminEventsHandler;
    private readonly ICommandHandler<CreateEventCommand, Result<CreateEventResult>> _createEventHandler;
    private readonly ICommandHandler<UpdateEventCommand, Result<UpdateEventResult>> _updateEventHandler;
    private readonly ICommandHandler<DeleteEventCommand, Result> _deleteEventHandler;

    public EventsController(
        IQueryHandler<GetEventsQuery, Result<EventsPagedResult>> getEventsHandler,
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
    /// Get events - supports tenant filtering and query parameters
    /// Query params: city, from (ISO date), to (ISO date), distanceKm (comma-separated), page, pageSize
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetEvents(
        [FromQuery] string? city = null,
        [FromQuery] string? from = null,
        [FromQuery] string? to = null,
        [FromQuery] string? distanceKm = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var tenantId = GetTenantId();

        // Parse date parameters - exact format: yyyy-MM-dd
        DateTimeOffset? dateFrom = null;
        DateTimeOffset? dateTo = null;

        if (!DateQueryParser.TryParseDateFilter(from, "from", false, out dateFrom, out var fromError))
        {
            return BadRequest(Calendarun.Common.Errors.ProblemDetailsFactory.Create(400, fromError ?? "Invalid date format", HttpContext));
        }

        if (!DateQueryParser.TryParseDateFilter(to, "to", true, out dateTo, out var toError))
        {
            return BadRequest(Calendarun.Common.Errors.ProblemDetailsFactory.Create(400, toError ?? "Invalid date format", HttpContext));
        }

        // Validate pagination
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var query = new GetEventsQuery(
            tenantId,
            city,
            dateFrom,
            dateTo,
            distanceKm,
            page,
            pageSize
        );

        var result = await _getEventsHandler.HandleAsync(query, ct);
        return ToActionResult(result, pagedResult => Ok(pagedResult));
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
    /// Get event as ICS calendar file
    /// </summary>
    [HttpGet("{id:guid}/ics")]
    public async Task<IActionResult> GetEventIcs(Guid id, CancellationToken ct)
    {
        var tenantId = GetTenantId();
        var result = await _getEventByIdHandler.HandleAsync(new GetEventByIdQuery(id, tenantId), ct);
        if (!result.IsSuccess)
        {
            return ToActionResult(result, Ok);
        }

        var eventDto = result.Value!;
        var icsContent = BuildIcs(eventDto);
        var fileName = $"{Slugify(eventDto.Title)}.ics";
        return File(Encoding.UTF8.GetBytes(icsContent), "text/calendar; charset=utf-8", fileName);
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
            return ToActionResult(result, _ => Ok(Array.Empty<string>()));

        var cities = result.Value!
            .Items
            .Select(e => e.City)
            .Where(c => !string.IsNullOrWhiteSpace(c))
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
            return BadRequest(Calendarun.Common.Errors.ProblemDetailsFactory.Create(
                400,
                $"{HeaderNames.TenantId} header is required",
                HttpContext));

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

    private static string BuildIcs(EventDto eventDto)
    {
        var nowUtc = DateTime.UtcNow;
        var startUtc = eventDto.StartAt.UtcDateTime;
        var uid = $"{eventDto.Id}@calendarun";
        var locationParts = new[] { eventDto.City, eventDto.CountryCode }
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .ToArray();
        var location = locationParts.Length > 0 ? string.Join(", ", locationParts) : string.Empty;

        var descriptionParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(eventDto.Description))
        {
            descriptionParts.Add(eventDto.Description);
        }
        if (!string.IsNullOrWhiteSpace(eventDto.RegistrationUrl))
        {
            descriptionParts.Add($"Registration: {eventDto.RegistrationUrl}");
        }
        var description = string.Join("\n\n", descriptionParts);

        var builder = new StringBuilder();
        builder.AppendLine("BEGIN:VCALENDAR");
        builder.AppendLine("VERSION:2.0");
        builder.AppendLine("PRODID:-//Calendarun//Events//EN");
        builder.AppendLine("CALSCALE:GREGORIAN");
        builder.AppendLine("METHOD:PUBLISH");
        builder.AppendLine("BEGIN:VEVENT");
        builder.AppendLine($"UID:{uid}");
        builder.AppendLine($"DTSTAMP:{FormatUtc(nowUtc)}");
        builder.AppendLine($"DTSTART:{FormatUtc(startUtc)}");
        builder.AppendLine($"SUMMARY:{EscapeIcs(eventDto.Title)}");
        if (!string.IsNullOrWhiteSpace(location))
        {
            builder.AppendLine($"LOCATION:{EscapeIcs(location)}");
        }
        if (!string.IsNullOrWhiteSpace(description))
        {
            builder.AppendLine($"DESCRIPTION:{EscapeIcs(description)}");
        }
        if (!string.IsNullOrWhiteSpace(eventDto.RegistrationUrl))
        {
            builder.AppendLine($"URL:{EscapeIcs(eventDto.RegistrationUrl)}");
        }
        builder.AppendLine("END:VEVENT");
        builder.AppendLine("END:VCALENDAR");

        return builder.ToString();
    }

    private static string FormatUtc(DateTime dateTime)
    {
        return dateTime.ToUniversalTime().ToString("yyyyMMdd'T'HHmmss'Z'");
    }

    private static string EscapeIcs(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("\r", string.Empty)
            .Replace("\n", "\\n")
            .Replace(";", "\\;")
            .Replace(",", "\\,");
    }

    private static string Slugify(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "event";
        }

        var builder = new StringBuilder();
        var previousDash = false;

        foreach (var ch in value.ToLowerInvariant())
        {
            if (ch <= 127 && char.IsLetterOrDigit(ch))
            {
                builder.Append(ch);
                previousDash = false;
                continue;
            }

            if (!previousDash)
            {
                builder.Append("-");
                previousDash = true;
            }
        }

        var slug = builder.ToString().Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? "event" : slug;
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
