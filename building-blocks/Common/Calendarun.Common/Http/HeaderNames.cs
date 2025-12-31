using Calendarun.Common.Auth;

namespace Calendarun.Common.Http;

public static class HeaderNames
{
    public const string TenantId = CalendarunClaimTypes.TenantId;
    public const string TenantRole = "X-Tenant-Role";
    public const string UserId = "X-User-Id";
    public const string UserEmail = "X-User-Email";
    public const string IsSuperAdmin = "X-Is-Super-Admin";
    public const string AcceptLanguage = "Accept-Language";
}
