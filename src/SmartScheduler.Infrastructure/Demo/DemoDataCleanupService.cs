using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartScheduler.Domain.Contracts.Entities;
using SmartScheduler.Infrastructure.Data;
using System.Diagnostics;
using EventLogEntity = SmartScheduler.Domain.Contracts.Entities.EventLog;

namespace SmartScheduler.Infrastructure.Demo;

/// <summary>
/// Result of demo data cleanup operation.
/// </summary>
public record CleanupResult(
    int ContractorsDeleted,
    int JobsDeleted,
    int AssignmentsDeleted,
    int AuditRecordsDeleted,
    int EventLogsDeleted,
    TimeSpan Duration
);

/// <summary>
/// Service for cleaning up all data from the database.
/// </summary>
public class DemoDataCleanupService
{
    private readonly SmartSchedulerDbContext _context;
    private readonly ILogger<DemoDataCleanupService> _logger;

    public DemoDataCleanupService(
        SmartSchedulerDbContext context,
        ILogger<DemoDataCleanupService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Deletes all data from the database (contractors, jobs, assignments, audit records, event logs).
    /// WARNING: This operation cannot be undone!
    /// </summary>
    public async Task<CleanupResult> DeleteAllDataAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        _logger.LogWarning("Starting database cleanup - deleting ALL data...");

        // Use transaction for atomicity
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        
        try
        {
            // Count records before deletion
            var contractorsCount = await _context.Set<Contractor>().CountAsync(cancellationToken);
            var jobsCount = await _context.Set<Job>().CountAsync(cancellationToken);
            var assignmentsCount = await _context.Set<Assignment>().CountAsync(cancellationToken);
            var auditRecordsCount = await _context.Set<AuditRecommendation>().CountAsync(cancellationToken);
            var eventLogsCount = await _context.Set<EventLogEntity>().CountAsync(cancellationToken);

            _logger.LogInformation("Found {Contractors} contractors, {Jobs} jobs, {Assignments} assignments, {Audits} audit records, {Events} event logs", 
                contractorsCount, jobsCount, assignmentsCount, auditRecordsCount, eventLogsCount);

            // Delete in correct order to respect foreign key constraints
            // 1. Delete assignments (references contractors and jobs)
            await _context.Database.ExecuteSqlRawAsync(
                "DELETE FROM \"Assignments\"", 
                cancellationToken);
            _logger.LogInformation("Deleted {Count} assignments", assignmentsCount);

            // 2. Delete audit recommendations (references jobs)
            await _context.Database.ExecuteSqlRawAsync(
                "DELETE FROM \"AuditRecommendations\"", 
                cancellationToken);
            _logger.LogInformation("Deleted {Count} audit records", auditRecordsCount);

            // 3. Delete event logs (no foreign keys)
            await _context.Database.ExecuteSqlRawAsync(
                "DELETE FROM \"EventLogs\"", 
                cancellationToken);
            _logger.LogInformation("Deleted {Count} event logs", eventLogsCount);

            // 4. Delete jobs (no dependencies)
            await _context.Database.ExecuteSqlRawAsync(
                "DELETE FROM \"Jobs\"", 
                cancellationToken);
            _logger.LogInformation("Deleted {Count} jobs", jobsCount);

            // 5. Delete contractors (no dependencies)
            await _context.Database.ExecuteSqlRawAsync(
                "DELETE FROM \"Contractors\"", 
                cancellationToken);
            _logger.LogInformation("Deleted {Count} contractors", contractorsCount);

            await transaction.CommitAsync(cancellationToken);
            
            stopwatch.Stop();
            
            _logger.LogWarning("Database cleanup completed in {Duration}ms - ALL DATA DELETED", stopwatch.ElapsedMilliseconds);

            return new CleanupResult(
                contractorsCount,
                jobsCount,
                assignmentsCount,
                auditRecordsCount,
                eventLogsCount,
                stopwatch.Elapsed
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during database cleanup");
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Gets counts of all data in the database without deleting.
    /// </summary>
    public async Task<(int contractors, int jobs, int assignments, int auditRecords, int eventLogs)> GetDataCountsAsync(
        CancellationToken cancellationToken = default)
    {
        var contractors = await _context.Set<Contractor>().CountAsync(cancellationToken);
        var jobs = await _context.Set<Job>().CountAsync(cancellationToken);
        var assignments = await _context.Set<Assignment>().CountAsync(cancellationToken);
        var auditRecords = await _context.Set<AuditRecommendation>().CountAsync(cancellationToken);
        var eventLogs = await _context.Set<EventLogEntity>().CountAsync(cancellationToken);

        return (contractors, jobs, assignments, auditRecords, eventLogs);
    }
}

