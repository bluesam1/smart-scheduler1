using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SmartScheduler.Infrastructure.Data.Seeding;

/// <summary>
/// Main database seeder that coordinates seeding operations.
/// </summary>
public class DatabaseSeeder
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(IServiceProvider serviceProvider, ILogger<DatabaseSeeder> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Seeds the database with development data.
    /// </summary>
    public async Task SeedDevelopmentDataAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SmartSchedulerDbContext>();
        var seeder = scope.ServiceProvider.GetRequiredService<DevelopmentDataSeeder>();

        await seeder.SeedAsync(context, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Seeds the database with test data.
    /// </summary>
    public async Task SeedTestDataAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SmartSchedulerDbContext>();
        var seeder = scope.ServiceProvider.GetRequiredService<TestDataSeeder>();

        await seeder.SeedAsync(context, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Clears development seed data.
    /// </summary>
    public async Task ClearDevelopmentDataAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SmartSchedulerDbContext>();
        var seeder = scope.ServiceProvider.GetRequiredService<DevelopmentDataSeeder>();

        await seeder.ClearAsync(context, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Clears test seed data.
    /// </summary>
    public async Task ClearTestDataAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SmartSchedulerDbContext>();
        var seeder = scope.ServiceProvider.GetRequiredService<TestDataSeeder>();

        await seeder.ClearAsync(context, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }
}

