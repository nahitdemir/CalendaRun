using Calendarun.Common.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using StackExchange.Redis;
using System.Security.Claims;
using System.Text.Json;
using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "Gateway")
    .WriteTo.Console(theme: AnsiConsoleTheme.Code)
    .CreateLogger();

builder.Host.UseSerilog();

// Configure Kestrel
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenLocalhost(8080);
});

// Add in-memory cache (L1 cache for membership validation)
builder.Services.AddMemoryCache();

// Add Redis (L2 cache)
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379"));

// Add HttpClient for Platform API
builder.Services.AddHttpClient("PlatformApi", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["PlatformApi:BaseUrl"] ?? "http://localhost:5401");
    client.Timeout = TimeSpan.FromSeconds(5);
});

// Add JWT Authentication (Keycloak)
var keycloakAuthority = builder.Configuration["Keycloak:Authority"] ?? "http://localhost:8180/realms/calendarun";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = keycloakAuthority;
        options.Audience = "calendarun-api";
        options.RequireHttpsMetadata = false; // Dev only
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = keycloakAuthority,
            // Audience validation: Keycloak public clients may not include audience in token
            // We validate that token is from our realm (issuer) and has valid signature
            // For public clients, audience is typically the client ID (azp claim)
            ValidateAudience = false, // Public clients don't always include audience
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(5), // Allow 5 minutes clock skew
            NameClaimType = CalendarunClaimTypes.PreferredUsername,
            RoleClaimType = CalendarunClaimTypes.RealmRoles
        };
    });

builder.Services.AddAuthorization();

// Add YARP with transforms
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms(builderContext =>
    {
        builderContext.AddRequestTransform(async transformContext =>
        {
            var httpContext = transformContext.HttpContext;
            
            // Forward validated tenant context
            if (httpContext.Items.TryGetValue("ValidatedTenantId", out var tenantId))
            {
                transformContext.ProxyRequest.Headers.Remove("X-Tenant-Id");
                transformContext.ProxyRequest.Headers.Add("X-Tenant-Id", tenantId?.ToString());
            }

            if (httpContext.Items.TryGetValue("TenantRole", out var role))
            {
                transformContext.ProxyRequest.Headers.Remove("X-Tenant-Role");
                transformContext.ProxyRequest.Headers.Add("X-Tenant-Role", role?.ToString());
            }

            // Forward user info from JWT
            if (httpContext.User.Identity?.IsAuthenticated == true)
            {
                var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) 
                    ?? httpContext.User.FindFirstValue("sub");
                var email = httpContext.User.FindFirstValue(ClaimTypes.Email) 
                    ?? httpContext.User.FindFirstValue("email");
                var isSuperAdmin = httpContext.User.HasClaim(CalendarunClaimTypes.RealmRoles, Roles.SuperAdmin);
                
                if (!string.IsNullOrEmpty(userId))
                {
                    transformContext.ProxyRequest.Headers.Remove("X-User-Id");
                    transformContext.ProxyRequest.Headers.Add("X-User-Id", userId);
                }
                
                if (!string.IsNullOrEmpty(email))
                {
                    transformContext.ProxyRequest.Headers.Remove("X-User-Email");
                    transformContext.ProxyRequest.Headers.Add("X-User-Email", email);
                }

                transformContext.ProxyRequest.Headers.Remove("X-Is-Super-Admin");
                transformContext.ProxyRequest.Headers.Add("X-Is-Super-Admin", isSuperAdmin.ToString());
            }

            await Task.CompletedTask;
        });
    });

// Add health checks
builder.Services.AddHealthChecks();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",  // Next.js dev
                "http://localhost:3001",  // Alternative port
                "http://127.0.0.1:3000"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

// CORS must be before authentication
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

// ==================== TENANT VALIDATION MIDDLEWARE ====================
app.Use(async (context, next) =>
{
    // Get path without query string for route matching
    var path = context.Request.Path.Value ?? "";
    var pathLower = path.ToLower();
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    
    // === PUBLIC ROUTES (no auth required) ===
    if (IsPublicRoute(pathLower))
    {
        await next();
        return;
    }

    // === SUPER ADMIN GLOBAL ROUTES (no tenant required, but auth required) ===
    if (IsSuperAdminRoute(path))
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "Authentication required" });
            return;
        }

        if (!context.User.HasClaim(CalendarunClaimTypes.RealmRoles, Roles.SuperAdmin))
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsJsonAsync(new { error = "Super admin role required" });
            return;
        }

        // Super admin accessing global routes - no tenant context needed
        context.Items["TenantRole"] = "SuperAdmin";
        await next();
        return;
    }

    // === USER SELF ROUTES (auth required, no tenant) ===
    if (IsUserSelfRoute(path))
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "Authentication required" });
            return;
        }
        
        await next();
        return;
    }

    // === TENANT-SCOPED ROUTES (auth + tenant membership required) ===
    if (IsTenantScopedRoute(path))
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "Authentication required" });
            return;
        }

        var tenantIdHeader = context.Request.Headers["X-Tenant-Id"].FirstOrDefault();
        
        // X-Tenant-Id is REQUIRED for tenant-scoped routes
        if (string.IsNullOrEmpty(tenantIdHeader))
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsJsonAsync(new { error = "X-Tenant-Id header is required for this endpoint" });
            return;
        }

        if (!Guid.TryParse(tenantIdHeader, out var tenantId))
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid X-Tenant-Id format" });
            return;
        }

        var userIdStr = context.User.FindFirstValue(ClaimTypes.NameIdentifier) 
            ?? context.User.FindFirstValue("sub");

        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid user identity in token" });
            return;
        }

        // Super admin bypasses tenant membership check
        if (context.User.HasClaim(CalendarunClaimTypes.RealmRoles, Roles.SuperAdmin))
        {
            context.Items["ValidatedTenantId"] = tenantId;
            context.Items["TenantRole"] = "SuperAdmin";
            logger.LogDebug("Super admin accessing tenant {TenantId}", tenantId);
            await next();
            return;
        }

        // Validate membership (L1 -> L2 -> Platform API)
        var validationResult = await ValidateMembershipAsync(context, userId, tenantId, logger);

        if (validationResult == null || !validationResult.IsMember)
        {
            logger.LogWarning("User {UserId} is not a member of tenant {TenantId}", userId, tenantId);
            context.Response.StatusCode = 403;
            await context.Response.WriteAsJsonAsync(new { 
                error = "User is not a member of this tenant",
                userId = userId,
                tenantId = tenantId
            });
            return;
        }

        context.Items["ValidatedTenantId"] = tenantId;
        context.Items["TenantRole"] = validationResult.Role;
        logger.LogDebug("User {UserId} validated for tenant {TenantId} with role {Role}", userId, tenantId, validationResult.Role);
    }

    await next();
});

// Health endpoint
app.MapHealthChecks("/health");

// Map reverse proxy
app.MapReverseProxy();

app.Run();

// ==================== ROUTE CLASSIFICATION ====================

static bool IsPublicRoute(string path)
{
    return path.StartsWith("/health") ||
           path.StartsWith("/invites/") || // Accept invite by token (public view)
           path.StartsWith("/api/invites/") ||
           path == "/api/events" || // Public event listing
           path.StartsWith("/api/events/") || // Public event details
           path == "/api/settings/event.distances"; // Public distance settings
}

static bool IsSuperAdminRoute(string path)
{
    return path.StartsWith("/super-admin/") ||
           path.StartsWith("/api/super-admin/");
}

static bool IsUserSelfRoute(string path)
{
    return path.StartsWith("/me/") ||
           path.StartsWith("/api/me/") ||
           path == "/api/me" ||
           path == "/invites/accept" ||
           path == "/api/invites/accept" ||
           // Plan is user-scoped, not tenant-scoped
           path == "/api/plan" ||
           path.StartsWith("/api/plan/");
}

static bool IsTenantScopedRoute(string path)
{
    // Admin routes require tenant context
    if (path.StartsWith("/admin/") || path.StartsWith("/api/admin/"))
        return true;

    // Settings routes are tenant-scoped, except public ones
    if (path.StartsWith("/api/settings") && !path.Contains("/super-admin/"))
    {
        // Exclude public settings routes
        if (path == "/api/settings/event.distances")
            return false;
        
        return true;
    }

    return false;
}

// ==================== MEMBERSHIP VALIDATION ====================

static async Task<MembershipValidationResult?> ValidateMembershipAsync(
    HttpContext context, 
    Guid userId, 
    Guid tenantId,
    Microsoft.Extensions.Logging.ILogger logger)
{
    var cacheKey = $"membership:{userId}:{tenantId}";
    
    // L1: In-memory cache (fastest, 1 minute TTL)
    var memoryCache = context.RequestServices.GetRequiredService<IMemoryCache>();
    if (memoryCache.TryGetValue(cacheKey, out MembershipValidationResult? cachedResult))
    {
        logger.LogDebug("Membership cache hit (L1) for {CacheKey}", cacheKey);
        return cachedResult;
    }

    // L2: Redis cache (fast, 5 minute TTL)
    var redis = context.RequestServices.GetRequiredService<IConnectionMultiplexer>();
    var redisDb = redis.GetDatabase();
    var redisValue = await redisDb.StringGetAsync(cacheKey);
    
    if (!redisValue.IsNullOrEmpty)
    {
        logger.LogDebug("Membership cache hit (L2/Redis) for {CacheKey}", cacheKey);
        var result = JsonSerializer.Deserialize<MembershipValidationResult>(redisValue!);
        
        // Populate L1 cache
        memoryCache.Set(cacheKey, result, TimeSpan.FromMinutes(1));
        return result;
    }

    // L3: Platform API (source of truth)
    logger.LogDebug("Membership cache miss, calling Platform API for {CacheKey}", cacheKey);
    
    var httpClientFactory = context.RequestServices.GetRequiredService<IHttpClientFactory>();
    var platformClient = httpClientFactory.CreateClient("PlatformApi");
    
    try
    {
        var response = await platformClient.GetAsync($"/api/memberships/validate?userId={userId}&tenantId={tenantId}");
        
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            var validationResult = JsonSerializer.Deserialize<MembershipValidationResult>(content, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            if (validationResult != null)
            {
                // Cache in L2 (Redis) for 5 minutes
                await redisDb.StringSetAsync(cacheKey, content, TimeSpan.FromMinutes(5));
                
                // Cache in L1 (Memory) for 1 minute
                memoryCache.Set(cacheKey, validationResult, TimeSpan.FromMinutes(1));
            }
            
            return validationResult;
        }
        else
        {
            logger.LogWarning("Platform API returned {StatusCode} for membership validation", response.StatusCode);
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to validate membership via Platform API");
    }

    return null;
}

record MembershipValidationResult(bool IsMember, string? Role);
