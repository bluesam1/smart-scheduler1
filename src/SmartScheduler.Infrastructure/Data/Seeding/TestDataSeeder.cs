using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace SmartScheduler.Infrastructure.Data.Seeding;

/// <summary>
/// Seeds the database with minimal test data for integration tests.
/// This seeder is idempotent and ensures test data is isolated.
/// </summary>
public class TestDataSeeder : IDataSeeder
{
    private readonly ILogger<TestDataSeeder> _logger;

    public TestDataSeeder(ILogger<TestDataSeeder> logger)
    {
        _logger = logger;
    }

    public async Task SeedAsync(SmartSchedulerDbContext context, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting test data seeding...");

        try
        {
            // Note: Actual seeding will be implemented once domain entities are created.
            // This structure is ready for minimal test data that ensures:
            // - Test isolation (each test can start with clean state)
            // - Minimal required data for test scenarios
            // - Fast test execution

            await Task.CompletedTask;
            _logger.LogInformation("Test data seeding completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during test data seeding.");
            throw;
        }
    }

    public async Task ClearAsync(SmartSchedulerDbContext context, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Clearing test seed data...");

        try
        {
            // Note: Actual clearing will be implemented once domain entities are created.
            // This will clear all test data to ensure test isolation.

            await Task.CompletedTask;
            _logger.LogInformation("Test seed data cleared successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during test data clearing.");
            throw;
        }
    }

    // TODO: Implement once domain entities exist:
    // - private async Task SeedMinimalTestDataAsync(SmartSchedulerDbContext context, CancellationToken cancellationToken)
}

