using Microsoft.EntityFrameworkCore;
using Platform.Domain.Entities;

namespace Platform.Infrastructure.Data;

public class PlatformDbContext : DbContext
{
    public PlatformDbContext(DbContextOptions<PlatformDbContext> options) : base(options)
    {
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Membership> Memberships => Set<Membership>();
    public DbSet<Invite> Invites => Set<Invite>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Slug).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.LogoUrl).HasMaxLength(500);
            entity.Property(e => e.DefaultLanguage).IsRequired().HasMaxLength(10).HasDefaultValue("tr");
            entity.Property(e => e.DefaultCurrency).IsRequired().HasMaxLength(10).HasDefaultValue("TRY");
            entity.Property(e => e.Status).IsRequired().HasConversion<string>().HasMaxLength(50);
            entity.HasIndex(e => e.Slug).IsUnique();
        });

        modelBuilder.Entity<Membership>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserEmail).IsRequired().HasMaxLength(250);
            entity.Property(e => e.Role).IsRequired().HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.Status).IsRequired().HasConversion<string>().HasMaxLength(50);
            entity.HasIndex(e => new { e.TenantId, e.UserId }).IsUnique();
            entity.HasIndex(e => e.UserId); // For /me/tenants query
            entity.HasOne(e => e.Tenant)
                  .WithMany(t => t.Memberships)
                  .HasForeignKey(e => e.TenantId);
        });

        modelBuilder.Entity<Invite>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(250);
            entity.Property(e => e.Token).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Role).IsRequired().HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.Status).IsRequired().HasConversion<string>().HasMaxLength(50);
            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasIndex(e => new { e.TenantId, e.Email, e.Status });
            entity.HasOne(e => e.Tenant)
                  .WithMany(t => t.Invites)
                  .HasForeignKey(e => e.TenantId);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(100);
            entity.Property(e => e.EntityType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ActorEmail).HasMaxLength(256);
            entity.Property(e => e.BeforeJson).HasColumnType("jsonb");
            entity.Property(e => e.AfterJson).HasColumnType("jsonb");
            entity.Property(e => e.TraceId).HasMaxLength(100);
            entity.Property(e => e.IpAddress).HasMaxLength(50);
            entity.HasIndex(e => new { e.TenantId, e.Timestamp });
            entity.HasIndex(e => new { e.ActorUserId, e.Timestamp });
        });

        // Seed data - using fixed timestamp for migration stability
        var seedTime = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        
        // Tenant1
        var tenant1Id = Guid.Parse("00000000-0000-0000-0000-000000000001");
        // admin@tenant1.local user ID (will be created in Keycloak)
        var adminTenant1UserId = Guid.Parse("00000000-0000-0000-0000-000000000010");

        modelBuilder.Entity<Tenant>().HasData(
            new Tenant
            {
                Id = tenant1Id,
                Name = "Tenant1",
                Slug = "tenant1",
                Description = "First tenant for development",
                DefaultLanguage = "tr",
                DefaultCurrency = "TRY",
                Status = TenantStatus.Active,
                CreatedAt = seedTime,
                CreatedBy = null // Created by system
            }
        );

        // admin@tenant1.local as tenant_admin in Tenant1
        modelBuilder.Entity<Membership>().HasData(
            new Membership
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                TenantId = tenant1Id,
                UserId = adminTenant1UserId,
                UserEmail = "admin@tenant1.local",
                Role = TenantRole.TenantAdmin,
                Status = MembershipStatus.Active,
                CreatedAt = seedTime,
                AcceptedAt = seedTime
            }
        );
    }
}
