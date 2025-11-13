using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SmartScheduler.Application.Contracts.DTOs;
using SmartScheduler.Application.Contracts.Queries;
using SmartScheduler.Domain.Contracts.Entities;
using SmartScheduler.Domain.Contracts.Repositories;
using SmartScheduler.Domain.Contracts.ValueObjects;

namespace SmartScheduler.Application.Contracts.Handlers;

/// <summary>
/// Handler for GetDashboardStatisticsQuery.
/// Calculates dashboard statistics with caching.
/// </summary>
public class GetDashboardStatisticsQueryHandler : IRequestHandler<GetDashboardStatisticsQuery, DashboardStatisticsDto>
{
    private readonly IContractorRepository _contractorRepository;
    private readonly IJobRepository _jobRepository;
    private readonly IAssignmentRepository _assignmentRepository;
    private readonly IMemoryCache _cache;
    private readonly ILogger<GetDashboardStatisticsQueryHandler> _logger;
    private const string CacheKey = "dashboard_statistics";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public GetDashboardStatisticsQueryHandler(
        IContractorRepository contractorRepository,
        IJobRepository jobRepository,
        IAssignmentRepository assignmentRepository,
        IMemoryCache cache,
        ILogger<GetDashboardStatisticsQueryHandler> logger)
    {
        _contractorRepository = contractorRepository;
        _jobRepository = jobRepository;
        _assignmentRepository = assignmentRepository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<DashboardStatisticsDto> Handle(
        GetDashboardStatisticsQuery request,
        CancellationToken cancellationToken)
    {
        // Check cache first
        if (_cache.TryGetValue<DashboardStatisticsDto>(CacheKey, out var cachedStats) && cachedStats != null)
        {
            _logger.LogDebug("Dashboard statistics retrieved from cache");
            return cachedStats;
        }

        _logger.LogDebug("Calculating dashboard statistics");

        // Calculate statistics
        var now = DateTime.UtcNow;
        var todayStart = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);
        var weekStart = todayStart.AddDays(-(int)now.DayOfWeek); // Start of week (Sunday)

        // Get all contractors (all are considered active)
        var allContractors = await _contractorRepository.GetAllAsync(cancellationToken);
        var activeContractorsCount = allContractors.Count;

        // Get contractors count from a week ago (for change indicator)
        // For MVP, we'll use a simple approach: count contractors created before today
        var contractorsToday = allContractors.Count(c => c.CreatedAt >= todayStart);
        var contractorsThisWeek = allContractors.Count(c => c.CreatedAt >= weekStart);
        var contractorsLastWeek = allContractors.Count(c => c.CreatedAt >= weekStart.AddDays(-7) && c.CreatedAt < weekStart);
        var contractorsChangeToday = contractorsToday;
        var contractorsChangeWeek = contractorsThisWeek - contractorsLastWeek;

        // Get pending jobs (Scheduled status with no assignment)
        var allJobs = await _jobRepository.GetAllAsync(cancellationToken);
        var pendingJobs = allJobs.Where(j => j.Status == JobStatus.Scheduled && j.AssignmentStatus == AssignmentStatus.Unassigned).ToList();
        var pendingJobsCount = pendingJobs.Count;
        var unassignedJobsCount = pendingJobs.Count(j => j.AssignmentStatus == AssignmentStatus.Unassigned);

        // Get pending jobs from a week ago
        var pendingJobsToday = pendingJobs.Count(j => j.CreatedAt >= todayStart);
        var pendingJobsThisWeek = pendingJobs.Count(j => j.CreatedAt >= weekStart);
        var pendingJobsLastWeek = allJobs.Count(j => j.Status == JobStatus.Scheduled && 
            j.AssignmentStatus == AssignmentStatus.Unassigned &&
            j.CreatedAt >= weekStart.AddDays(-7) && j.CreatedAt < weekStart);
        var pendingJobsChangeToday = pendingJobsToday;
        var pendingJobsChangeWeek = pendingJobsThisWeek - pendingJobsLastWeek;

        // Calculate average assignment time
        var assignedJobs = allJobs.Where(j => j.AssignedContractors.Any()).ToList();
        var assignmentTimes = new List<TimeSpan>();
        
        foreach (var job in assignedJobs)
        {
            // Get assignments for this job
            var assignments = await _assignmentRepository.GetByJobIdAsync(job.Id, cancellationToken);
            if (assignments.Any())
            {
                var firstAssignment = assignments.OrderBy(a => a.CreatedAt).First();
                var timeToAssignment = firstAssignment.CreatedAt - job.CreatedAt;
                if (timeToAssignment.TotalMinutes > 0)
                {
                    assignmentTimes.Add(timeToAssignment);
                }
            }
        }

        var averageAssignmentTimeMinutes = assignmentTimes.Any() 
            ? (int)assignmentTimes.Average(t => t.TotalMinutes)
            : 0;

        // For change indicator, we'd need historical data - for MVP, we'll skip this
        // or calculate based on recent assignments
        var recentAssignments = assignedJobs.Where(j => j.UpdatedAt >= weekStart).ToList();
        var oldAssignments = assignedJobs.Where(j => j.UpdatedAt >= weekStart.AddDays(-7) && j.UpdatedAt < weekStart).ToList();
        
        var recentTimes = new List<double>();
        foreach (var job in recentAssignments)
        {
            var assignments = await _assignmentRepository.GetByJobIdAsync(job.Id, cancellationToken);
            if (assignments.Any())
            {
                var firstAssignment = assignments.OrderBy(a => a.CreatedAt).First();
                var timeDiff = (firstAssignment.CreatedAt - job.CreatedAt).TotalMinutes;
                if (timeDiff > 0)
                {
                    recentTimes.Add(timeDiff);
                }
            }
        }
        var recentAvgTime = recentTimes.Any() ? recentTimes.Average() : 0.0;

        var oldTimes = new List<double>();
        foreach (var job in oldAssignments)
        {
            var assignments = await _assignmentRepository.GetByJobIdAsync(job.Id, cancellationToken);
            if (assignments.Any())
            {
                var firstAssignment = assignments.OrderBy(a => a.CreatedAt).First();
                var timeDiff = (firstAssignment.CreatedAt - job.CreatedAt).TotalMinutes;
                if (timeDiff > 0)
                {
                    oldTimes.Add(timeDiff);
                }
            }
        }
        var oldAvgTime = oldTimes.Any() ? oldTimes.Average() : 0.0;

        var assignmentTimeChange = recentAvgTime > 0 && oldAvgTime > 0 
            ? (int)(recentAvgTime - oldAvgTime)
            : 0;

        // Calculate utilization rate
        // Utilization = (total assigned time) / (total available working time) * 100
        // Optimized: Get all active assignments in one query instead of N+1
        var contractorIds = allContractors.Select(c => c.Id).ToList();
        var allActiveAssignments = await _assignmentRepository.GetActiveAssignmentsByContractorIdsAsync(
            contractorIds, 
            cancellationToken);
        
        // Group assignments by contractor for efficient lookup
        var assignmentsByContractor = allActiveAssignments
            .GroupBy(a => a.ContractorId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var totalAssignedMinutes = 0;
        var totalAvailableMinutes = 0;

        foreach (var contractor in allContractors)
        {
            // Get assignments for this contractor from the grouped dictionary
            var activeAssignments = assignmentsByContractor.GetValueOrDefault(contractor.Id, new List<Assignment>()) ?? new List<Assignment>();

            // Calculate assigned time (sum of durations)
            foreach (var assignment in activeAssignments)
            {
                totalAssignedMinutes += (int)(assignment.EndUtc - assignment.StartUtc).TotalMinutes;
            }

            // Calculate available time from working hours
            // For MVP, we'll use a simplified calculation: assume 8 hours per day, 5 days per week
            // This is a placeholder - in production, this should use actual working hours
            var workingDaysPerWeek = contractor.WorkingHours.Count;
            var hoursPerDay = contractor.WorkingHours.Any() 
                ? contractor.WorkingHours.Average(wh => (wh.EndTime - wh.StartTime).TotalHours)
                : 8.0;
            var weeklyAvailableMinutes = (int)(workingDaysPerWeek * hoursPerDay * 60);
            totalAvailableMinutes += weeklyAvailableMinutes;
        }

        var utilizationRate = totalAvailableMinutes > 0 
            ? (totalAssignedMinutes / (double)totalAvailableMinutes) * 100.0
            : 0.0;

        // For utilization change, we'd need historical data - skip for MVP
        var utilizationChange = 0.0;

        // Build response
        var statistics = new DashboardStatisticsDto
        {
            ActiveContractors = new StatMetric
            {
                Value = activeContractorsCount,
                ChangeIndicator = FormatChangeIndicator(contractorsChangeToday, contractorsChangeWeek)
            },
            PendingJobs = new JobStatMetric
            {
                Value = pendingJobsCount,
                Unassigned = unassignedJobsCount,
                ChangeIndicator = FormatChangeIndicator(pendingJobsChangeToday, pendingJobsChangeWeek)
            },
            AverageAssignmentTime = new TimeMetric
            {
                ValueMinutes = averageAssignmentTimeMinutes,
                ChangeIndicator = assignmentTimeChange != 0 ? FormatTimeChangeIndicator(assignmentTimeChange) : null
            },
            UtilizationRate = new PercentMetric
            {
                Value = Math.Round(utilizationRate, 1),
                ChangeIndicator = utilizationChange != 0 ? FormatPercentChangeIndicator(utilizationChange) : null
            }
        };

        // Cache the result
        _cache.Set(CacheKey, statistics, CacheTtl);

        return statistics;
    }

    private static string? FormatChangeIndicator(int todayChange, int weekChange)
    {
        if (todayChange == 0 && weekChange == 0)
            return null;

        if (todayChange != 0)
        {
            var sign = todayChange > 0 ? "+" : "";
            return $"{sign}{todayChange} today";
        }

        if (weekChange != 0)
        {
            var sign = weekChange > 0 ? "+" : "";
            return $"{sign}{weekChange} this week";
        }

        return null;
    }

    private static string FormatTimeChangeIndicator(int changeMinutes)
    {
        var sign = changeMinutes > 0 ? "+" : "";
        var hours = Math.Abs(changeMinutes) / 60;
        var minutes = Math.Abs(changeMinutes) % 60;
        
        if (hours > 0)
            return $"{sign}{hours}h {minutes}m";
        
        return $"{sign}{minutes}m";
    }

    private static string FormatPercentChangeIndicator(double change)
    {
        var sign = change > 0 ? "+" : "";
        return $"{sign}{change:F1}%";
    }
}

