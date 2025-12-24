using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Platform.Infrastructure.Data;

/// <summary>
/// Factory for creating PlatformDbContext at design time (EF migrations).
/// This bypasses the need for Redis and other runtime dependencies.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<PlatformDbContext>
{
    public PlatformDbContext CreateDbContext(string[] args)
    {
        // Try to load from environment variable first
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
            ?? Environment.GetEnvironmentVariable("DATABASE_URL");

        // Fallback to appsettings if not in environment
        if (string.IsNullOrEmpty(connectionString))
        {
            // Look for appsettings.json in the API project
            var basePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "Platform.Api");
            
            if (Directory.Exists(basePath))
            {
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(basePath)
                    .AddJsonFile("appsettings.json", optional: true)
                    .AddJsonFile("appsettings.Development.json", optional: true)
                    .Build();

                connectionString = configuration.GetConnectionString("Postgres");
            }
        }

        // Ultimate fallback for local development
        connectionString ??= "Host=localhost;Port=5432;Database=calendarun_platform;Username=calendarun;Password=calendarun";

        var optionsBuilder = new DbContextOptionsBuilder<PlatformDbContext>();
        optionsBuilder.UseNpgsql(connectionString, options =>
        {
            options.MigrationsAssembly("Platform.Infrastructure");
        });

        return new PlatformDbContext(optionsBuilder.Options);
    }
}

