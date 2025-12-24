using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Platform.Application.Common;
using Platform.Domain.Entities;
using Platform.Infrastructure.Data;
using System.Text.Json;

namespace Platform.Application.Tenants.Commands;

public class CreateTenantHandler : ICommandHandler<CreateTenantCommand, Result<CreateTenantResult>>
{
    private readonly PlatformDbContext _db;
    private readonly ILogger<CreateTenantHandler> _logger;

    public CreateTenantHandler(PlatformDbContext db, ILogger<CreateTenantHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Result<CreateTenantResult>> HandleAsync(CreateTenantCommand command, CancellationToken ct = default)
    {
        var slug = command.Slug ?? command.Name.ToLower().Replace(" ", "-");
        
        // Check slug uniqueness
        var existingTenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Slug == slug, ct);
        if (existingTenant != null)
        {
            return Result<CreateTenantResult>.Conflict("Tenant with this slug already exists");
        }

        var now = DateTimeOffset.UtcNow;
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = command.Name,
            Slug = slug,
            Description = command.Description,
            DefaultLanguage = command.DefaultLanguage ?? "tr",
            DefaultCurrency = command.DefaultCurrency ?? "TRY",
            Status = TenantStatus.Active,
            CreatedAt = now,
            CreatedBy = command.ActorUserId
        };

        _db.Tenants.Add(tenant);

        // Add audit log
        _db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            ActorUserId = command.ActorUserId,
            ActorEmail = command.ActorEmail,
            Action = "TenantCreated",
            EntityType = "Tenant",
            EntityId = tenant.Id,
            AfterJson = JsonSerializer.Serialize(new { tenant.Name, tenant.Slug }),
            Timestamp = now
        });

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Tenant created: {TenantId} {TenantSlug} by {UserId}", 
            tenant.Id, tenant.Slug, command.ActorUserId);

        return Result<CreateTenantResult>.Success(new CreateTenantResult(
            tenant.Id,
            tenant.Name,
            tenant.Slug,
            tenant.DefaultLanguage,
            tenant.DefaultCurrency,
            tenant.CreatedAt
        ));
    }
}

