using Catalog.Domain;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure;

public class CatalogDbContext : DbContext
{
    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options)
    {
    }

    public DbSet<Event> Events => Set<Event>();

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
        });

        // Seed data - events starting soon (for demo/testing)
        // Using fixed dates for migration stability
        var seedTime = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        
        modelBuilder.Entity<Event>().HasData(
            new Event
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Title = "Istanbul Marathon 2025",
                Description = "44th Istanbul Marathon - intercontinental running experience",
                StartAt = new DateTimeOffset(2025, 1, 5, 8, 0, 0, TimeSpan.FromHours(3)), // 5 days from seed
                City = "Istanbul",
                CountryCode = "TR",
                RegistrationUrl = "https://istanbulmarathon.org",
                CreatedAt = seedTime
            },
            new Event
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Title = "Cappadocia Ultra Trail",
                Description = "100km trail run through fairy chimneys",
                StartAt = new DateTimeOffset(2025, 1, 3, 7, 0, 0, TimeSpan.FromHours(3)), // 3 days from seed
                City = "Nevşehir",
                CountryCode = "TR",
                RegistrationUrl = "https://cappadociaultra.com",
                CreatedAt = seedTime
            },
            new Event
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Title = "Antalya Half Marathon",
                Description = "Scenic coastal run along Turkish Riviera",
                StartAt = new DateTimeOffset(2025, 1, 2, 9, 0, 0, TimeSpan.FromHours(3)), // Tomorrow from seed
                City = "Antalya",
                CountryCode = "TR",
                RegistrationUrl = "https://antalyahalf.com",
                CreatedAt = seedTime
            }
        );
    }
}
