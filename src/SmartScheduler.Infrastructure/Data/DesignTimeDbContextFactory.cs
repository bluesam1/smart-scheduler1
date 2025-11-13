using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace SmartScheduler.Infrastructure.Data;

/// <summary>
/// Design-time factory for EF Core migrations.
/// This allows migrations to be created without running the application.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<SmartSchedulerDbContext>
{
    public SmartSchedulerDbContext CreateDbContext(string[] args)
    {
        // Build configuration from appsettings.json
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../SmartScheduler.Api"))
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrEmpty(connectionString))
        {
            // Fallback connection string for design-time (migrations can be created without a real database)
            connectionString = "Host=localhost;Port=5432;Database=smartscheduler;Username=postgres;Password=postgres";
        }

        var optionsBuilder = new DbContextOptionsBuilder<SmartSchedulerDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsqlOptions => npgsqlOptions.UseNetTopologySuite());

        return new SmartSchedulerDbContext(optionsBuilder.Options);
    }
}

