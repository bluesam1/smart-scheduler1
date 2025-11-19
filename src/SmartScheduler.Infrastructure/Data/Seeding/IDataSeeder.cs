namespace SmartScheduler.Infrastructure.Data.Seeding;

/// <summary>
/// Interface for data seeders that can populate the database with initial or test data.
/// </summary>
public interface IDataSeeder
{
    /// <summary>
    /// Seeds the database with data. Should be idempotent.
    /// </summary>
    /// <param name="context">The database context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SeedAsync(SmartSchedulerDbContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears seeded data from the database.
    /// </summary>
    /// <param name="context">The database context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ClearAsync(SmartSchedulerDbContext context, CancellationToken cancellationToken = default);
}




