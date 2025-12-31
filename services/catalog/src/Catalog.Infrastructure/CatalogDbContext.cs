using Calendarun.Common;
using Catalog.Domain;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure;

public class CatalogDbContext : DbContext
{
    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options)
    {
    }

    public DbSet<Event> Events => Set<Event>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
            entity.Property(e => e.City).IsRequired().HasMaxLength(200);
            entity.Property(e => e.CountryCode).IsRequired().HasMaxLength(10);
            entity.Property(e => e.RegistrationUrl).HasMaxLength(1000);
            entity.Property(e => e.Distances)
                .HasColumnType("integer[]"); // PostgreSQL array type
            
            // Tenant isolation index
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => new { e.TenantId, e.StartAt });
            // GIN index for array search performance
            entity.HasIndex(e => e.Distances)
                .HasMethod("gin");
        });

        // AuditLog
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

        // Seed data - events starting soon (for demo/testing)
        // Using fixed dates for migration stability
        var seedTime = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var defaultTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        
        modelBuilder.Entity<Event>().HasData(
            new Event
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                TenantId = null, // Global event
                Title = "Istanbul Marathon 2025",
                Description = "44th Istanbul Marathon - intercontinental running experience",
                StartAt = new DateTimeOffset(2025, 1, 5, 8, 0, 0, TimeSpan.FromHours(3)),
                City = "Istanbul",
                CountryCode = Defaults.CountryCode,
                RegistrationUrl = "https://istanbulmarathon.org",
                Distances = new[] { 10, 21, 42 }, // 10K, Half Marathon, Full Marathon
                CreatedAt = seedTime
            },
            new Event
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                TenantId = null, // Global event
                Title = "Cappadocia Ultra Trail",
                Description = "100km trail run through fairy chimneys",
                StartAt = new DateTimeOffset(2025, 1, 3, 7, 0, 0, TimeSpan.FromHours(3)),
                City = "Nevşehir",
                CountryCode = Defaults.CountryCode,
                RegistrationUrl = "https://cappadociaultra.com",
                Distances = new[] { 100 }, // Ultra (100km)
                CreatedAt = seedTime
            },
            new Event
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                TenantId = defaultTenantId, // Default tenant event
                Title = "Antalya Half Marathon",
                Description = "Scenic coastal run along Turkish Riviera",
                StartAt = new DateTimeOffset(2025, 1, 2, 9, 0, 0, TimeSpan.FromHours(3)),
                City = "Antalya",
                CountryCode = Defaults.CountryCode,
                RegistrationUrl = "https://antalyahalf.com",
                Distances = new[] { 5, 10, 21 }, // 5K, 10K, Half Marathon
                CreatedAt = seedTime
            }
        );
    }
}
