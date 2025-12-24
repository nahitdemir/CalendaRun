using Calendarun.Common.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Domain.Entities;
using Platform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Platform.Api.Controllers;

/// <summary>
/// Current user profile and tenant memberships
/// </summary>
[ApiController]
[Authorize]
public class MeController : ControllerBase
{
    private readonly PlatformDbContext _db;

    public MeController(PlatformDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Get current user profile
    /// </summary>
    [HttpGet("/api/me")]
    public IActionResult GetMe()
    {
        var userId = GetUserId();
        var email = GetUserEmail();
        var name = GetUserName();
        var roles = GetUserRoles();
        var isSuperAdmin = IsSuperAdmin();

        return Ok(new
        {
            id = userId.ToString(),
            email,
            name,
            roles,
            isSuperAdmin
        });
    }

    /// <summary>
    /// Get current user's tenant memberships
    /// </summary>
    [HttpGet("/api/me/tenants")]
    public async Task<IActionResult> GetMyTenants(CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
            return Ok(new List<object>());

        var memberships = await _db.Memberships
            .Include(m => m.Tenant)
            .Where(m => m.UserId == userId && m.Status == MembershipStatus.Active)
            .Select(m => new
            {
                tenantId = m.TenantId.ToString(),
                tenantName = m.Tenant!.Name,
                tenantSlug = m.Tenant.Slug,
                role = m.Role.ToString(),
                createdAt = m.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(memberships);
    }

    private Guid GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }

    private string GetUserEmail()
    {
        return User.FindFirstValue(ClaimTypes.Email) ?? 
               User.FindFirstValue("email") ?? 
               "";
    }

    private string? GetUserName()
    {
        return User.FindFirstValue(ClaimTypes.Name) ?? 
               User.FindFirstValue("name") ??
               User.FindFirstValue("preferred_username");
    }

    private List<string> GetUserRoles()
    {
        var roles = new List<string>();
        
        // Get realm roles
        var realmRoles = User.FindAll(CalendarunClaimTypes.RealmRoles).Select(c => c.Value);
        roles.AddRange(realmRoles);
        
        // Get resource roles if present
        var resourceRoles = User.FindAll("resource_access").Select(c => c.Value);
        roles.AddRange(resourceRoles);
        
        return roles.Distinct().ToList();
    }

    private bool IsSuperAdmin()
    {
        return User.HasClaim(CalendarunClaimTypes.RealmRoles, Roles.SuperAdmin) ||
               User.IsInRole(Roles.SuperAdmin);
    }
}

