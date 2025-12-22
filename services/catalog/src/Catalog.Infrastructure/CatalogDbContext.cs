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

        // Seed data
        var now = DateTimeOffset.UtcNow;
        modelBuilder.Entity<Event>().HasData(
            new Event
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Title = "Istanbul Marathon 2025",
                StartAt = new DateTimeOffset(2025, 11, 2, 8, 0, 0, TimeSpan.FromHours(3)),
                City = "Istanbul",
                CountryCode = "TR",
                RegistrationUrl = "https://istanbulmarathon.org",
                CreatedAt = now
            },
            new Event
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Title = "Cappadocia Ultra Trail",
                StartAt = new DateTimeOffset(2025, 10, 15, 7, 0, 0, TimeSpan.FromHours(3)),
                City = "Nevşehir",
                CountryCode = "TR",
                RegistrationUrl = "https://cappadociaultra.com",
                CreatedAt = now
            },
            new Event
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Title = "Antalya Half Marathon",
                StartAt = new DateTimeOffset(2025, 12, 20, 9, 0, 0, TimeSpan.FromHours(3)),
                City = "Antalya",
                CountryCode = "TR",
                RegistrationUrl = "https://antalyahalf.com",
                CreatedAt = now
            }
        );
    }
}

