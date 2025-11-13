using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartScheduler.Domain.Contracts.Entities;
using SmartScheduler.Domain.Contracts.Repositories;
using SmartScheduler.Domain.Contracts.ValueObjects;
using SmartScheduler.Infrastructure.Data;
using System.Diagnostics;
using System.Text.Json;

namespace SmartScheduler.Infrastructure.Demo;

/// <summary>
/// Result of demo data generation operation.
/// </summary>
public record DemoDataResult(
    int ContractorsCreated,
    int JobsCreated,
    int AssignmentsCreated,
    int AuditRecordsCreated,
    TimeSpan Duration
);

/// <summary>
/// Service for generating realistic demo data across multiple US timezones.
/// </summary>
public class DemoDataService
{
    private readonly SmartSchedulerDbContext _context;
    private readonly ISystemConfigurationRepository _configRepository;
    private readonly IWeightsConfigRepository _weightsRepository;
    private readonly ILogger<DemoDataService> _logger;
    private readonly AddressData _addressData;
    private readonly Random _random;

    public DemoDataService(
        SmartSchedulerDbContext context,
        ISystemConfigurationRepository configRepository,
        IWeightsConfigRepository weightsRepository,
        ILogger<DemoDataService> logger)
    {
        _context = context;
        _configRepository = configRepository;
        _weightsRepository = weightsRepository;
        _logger = logger;
        _addressData = new AddressData();
        
        // Use timestamp-based seed for variety across runs
        _random = new Random((int)DateTime.UtcNow.Ticks);
    }

    /// <summary>
    /// Generates realistic demo data and saves to database.
    /// </summary>
    public async Task<DemoDataResult> GenerateDemoDataAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        _logger.LogInformation("Starting demo data generation...");

        // Load system configuration
        var jobTypesConfig = await _configRepository.GetByTypeAsync(ConfigurationType.JobTypes, cancellationToken);
        var skillsConfig = await _configRepository.GetByTypeAsync(ConfigurationType.Skills, cancellationToken);
        var weightsConfig = await _weightsRepository.GetActiveAsync(cancellationToken);

        if (jobTypesConfig == null || skillsConfig == null)
        {
            throw new InvalidOperationException("System configuration (job types and skills) must be initialized before generating demo data.");
        }

        var jobTypes = jobTypesConfig.ValuesReadOnly.ToList();
        var skills = skillsConfig.ValuesReadOnly.ToList();
        var currentConfigVersion = weightsConfig?.Version ?? 1;

        var timestamp = DateTime.UtcNow;

        // Use transaction for atomicity
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        
        try
        {
            // Generate contractors
            var contractors = GenerateContractors(skills, timestamp);
            await _context.Set<Contractor>().AddRangeAsync(contractors, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Generated {Count} contractors", contractors.Count);

            // Clear domain events to avoid performance issues
            foreach (var contractor in contractors)
            {
                contractor.ClearDomainEvents();
            }

            // Generate jobs
            var jobs = GenerateJobs(jobTypes, skills, timestamp);
            await _context.Set<Job>().AddRangeAsync(jobs, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Generated {Count} jobs", jobs.Count);

            // Clear domain events
            foreach (var job in jobs)
            {
                job.ClearDomainEvents();
            }

            // Generate assignments and audit records
            var (assignments, auditRecords) = GenerateAssignmentsAndAudits(
                jobs, 
                contractors, 
                currentConfigVersion, 
                timestamp);

            await _context.Set<Assignment>().AddRangeAsync(assignments, cancellationToken);
            await _context.Set<AuditRecommendation>().AddRangeAsync(auditRecords, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("Generated {AssignmentCount} assignments and {AuditCount} audit records", 
                assignments.Count, auditRecords.Count);

            await transaction.CommitAsync(cancellationToken);
            
            stopwatch.Stop();
            
            _logger.LogInformation("Demo data generation completed in {Duration}ms", stopwatch.ElapsedMilliseconds);

            return new DemoDataResult(
                contractors.Count,
                jobs.Count,
                assignments.Count,
                auditRecords.Count,
                stopwatch.Elapsed
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating demo data");
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private List<Contractor> GenerateContractors(List<string> skills, DateTime timestamp)
    {
        var contractorCount = _random.Next(50, 101); // 50-100 contractors
        var contractors = new List<Contractor>();

        for (int i = 0; i < contractorCount; i++)
        {
            var address = _addressData.GetRandomAddress(_random);
            var geoLocation = _addressData.ToGeoLocation(address);
            
            // Generate contractor with unique timestamp-based name
            var contractorTimestamp = timestamp.AddSeconds(i);
            var name = NameGenerator.GenerateUniqueName(_random, contractorTimestamp);
            
            // Random skills (2-5 skills per contractor)
            var contractorSkills = skills
                .OrderBy(_ => _random.Next())
                .Take(_random.Next(2, 6))
                .ToList();

            // Varied ratings (30-95) to create realistic distribution
            var rating = _random.Next(30, 96);

            // Standard M-F 8am-5pm working hours in contractor's timezone
            var workingHours = GenerateWorkingHours(address.Timezone);

            // Vary max jobs per day (3-5)
            var maxJobsPerDay = _random.Next(3, 6);

            // Backdate creation (6-12 months ago)
            var createdAt = timestamp.AddMonths(-_random.Next(6, 13));

            var contractor = new Contractor(
                Guid.NewGuid(),
                name,
                geoLocation,
                workingHours,
                contractorSkills,
                rating,
                null, // No calendar exceptions for demo
                maxJobsPerDay
            );

            contractors.Add(contractor);
        }

        return contractors;
    }

    private List<WorkingHours> GenerateWorkingHours(string timezone)
    {
        var workingHours = new List<WorkingHours>();
        
        // Monday-Friday, 8am-5pm
        for (int day = 1; day <= 5; day++)
        {
            workingHours.Add(new WorkingHours(
                (DayOfWeek)day,
                new TimeOnly(8, 0),
                new TimeOnly(17, 0),
                timezone
            ));
        }

        return workingHours;
    }

    private List<Job> GenerateJobs(List<string> jobTypes, List<string> skills, DateTime timestamp)
    {
        var jobCount = _random.Next(200, 501); // 200-500 jobs
        var jobs = new List<Job>();

        // Job descriptions templates
        var descriptionTemplates = new[]
        {
            "Install {type} in {area}",
            "{type} installation for {area}",
            "Replace existing flooring with {type}",
            "{type} renovation - {detail}",
            "Complete {type} installation",
            "Emergency {type} repair",
            "{type} maintenance and inspection"
        };

        var areas = new[] { "living room", "master bedroom", "kitchen", "3 bedrooms", "entire first floor", "basement", "office space", "1,200 sq ft area", "2,000 sq ft area" };
        var details = new[] { "high priority", "standard installation", "custom work", "includes preparation", "full service" };

        var accessNotes = new[]
        {
            "Gate code: #1234",
            "Park in driveway, ring doorbell",
            "Side entrance only",
            "Call on arrival",
            "Key available at office",
            "Building access code: 5678",
            "Park in visitor parking",
            "Superintendent will provide access"
        };

        for (int i = 0; i < jobCount; i++)
        {
            var address = _addressData.GetRandomAddress(_random);
            var geoLocation = _addressData.ToGeoLocation(address);

            var jobType = jobTypes[_random.Next(jobTypes.Count)];
            
            // Generate description
            var template = descriptionTemplates[_random.Next(descriptionTemplates.Length)];
            var description = template
                .Replace("{type}", jobType.ToLower())
                .Replace("{area}", areas[_random.Next(areas.Length)])
                .Replace("{detail}", details[_random.Next(details.Length)]);

            // Random required skills (1-3 skills)
            var requiredSkills = skills
                .OrderBy(_ => _random.Next())
                .Take(_random.Next(1, 4))
                .ToList();

            // Duration: 60-480 minutes (1-8 hours)
            var duration = _random.Next(60, 481);

            // Priority distribution: Normal 70%, High 20%, Rush 10%
            var priorityRoll = _random.Next(100);
            var priority = priorityRoll < 70 ? Priority.Normal : priorityRoll < 90 ? Priority.High : Priority.Rush;

            // Historical dates over past 6 months
            var daysAgo = _random.Next(1, 181);
            var desiredDate = timestamp.AddDays(-daysAgo).Date;
            
            // Service window (8 hours during business hours)
            var startHour = _random.Next(7, 16); // 7am-4pm start
            var serviceWindowStart = new DateTime(desiredDate.Year, desiredDate.Month, desiredDate.Day, startHour, 0, 0, DateTimeKind.Utc);
            var serviceWindowEnd = serviceWindowStart.AddHours(8);

            var job = new Job(
                Guid.NewGuid(),
                jobType,
                duration,
                geoLocation,
                address.Timezone,
                new TimeWindow(serviceWindowStart, serviceWindowEnd),
                priority,
                desiredDate,
                requiredSkills,
                description,
                accessNotes[_random.Next(accessNotes.Length)],
                null // No tools for demo
            );

            jobs.Add(job);
        }

        return jobs;
    }

    private (List<Assignment> assignments, List<AuditRecommendation> auditRecords) GenerateAssignmentsAndAudits(
        List<Job> jobs,
        List<Contractor> contractors,
        int configVersion,
        DateTime timestamp)
    {
        var assignments = new List<Assignment>();
        var auditRecords = new List<AuditRecommendation>();

        foreach (var job in jobs)
        {
            // Determine if this job should be cancelled (5% chance)
            var shouldCancel = _random.Next(100) < 5;
            if (shouldCancel)
            {
                job.Cancel("Demo cancellation");
                continue;
            }

            // Determine if this job should get an assignment (85% chance)
            // This leaves ~15% unassigned + 5% cancelled = ~20% without active assignments
            var shouldAssign = _random.Next(100) < 85;
            if (!shouldAssign)
            {
                // Leave job in Created status (unassigned)
                continue;
            }

            // Find compatible contractors (those with matching skills)
            var compatibleContractors = contractors
                .Where(c => job.RequiredSkills.All(skill => c.Skills.Contains(skill)))
                .ToList();

            if (compatibleContractors.Count == 0)
            {
                // No compatible contractors, leave job unassigned
                continue;
            }

            // Generate candidate scores for audit
            var candidates = GenerateCandidateScores(job, compatibleContractors);

            // Select contractor (weighted by score)
            var selectedContractor = SelectContractorWeighted(compatibleContractors, candidates);

            // Generate assignment times
            var startUtc = job.ServiceWindow.Start.AddHours(_random.Next(0, 2));
            var endUtc = startUtc.AddMinutes(job.Duration);

            // Assignment source: 60% auto, 40% manual (showing system is working)
            var source = _random.Next(100) < 60 ? AssignmentSource.Auto : AssignmentSource.Manual;

            // Determine assignment status based on job timeline
            // Completed: jobs more than 7 days old (60%)
            // InProgress: jobs 1-7 days old (10%)
            // Confirmed: future jobs (30%)
            AssignmentEntityStatus assignmentStatus;
            JobStatus jobStatus;
            
            var daysFromNow = (job.DesiredDate - timestamp.Date).Days;
            
            if (daysFromNow < -7)
            {
                // Old jobs should be completed
                assignmentStatus = AssignmentEntityStatus.Completed;
                jobStatus = JobStatus.Completed;
            }
            else if (daysFromNow < 0 && daysFromNow >= -7)
            {
                // Recent past jobs could be in progress or completed
                var isCompleted = _random.Next(100) < 70; // 70% completed, 30% in progress
                if (isCompleted)
                {
                    assignmentStatus = AssignmentEntityStatus.Completed;
                    jobStatus = JobStatus.Completed;
                }
                else
                {
                    assignmentStatus = AssignmentEntityStatus.InProgress;
                    jobStatus = JobStatus.InProgress;
                }
            }
            else
            {
                // Future jobs should be confirmed/assigned
                assignmentStatus = AssignmentEntityStatus.Confirmed;
                jobStatus = JobStatus.Assigned;
            }

            // Create the assignment
            var assignment = new Assignment(
                Guid.NewGuid(),
                job.Id,
                selectedContractor.Id,
                startUtc,
                endUtc,
                source
            );

            // Update assignment status to match determined status
            if (assignmentStatus == AssignmentEntityStatus.Confirmed)
            {
                assignment.Confirm();
            }
            else if (assignmentStatus == AssignmentEntityStatus.InProgress)
            {
                assignment.Confirm();
                assignment.MarkInProgress();
            }
            else if (assignmentStatus == AssignmentEntityStatus.Completed)
            {
                assignment.Confirm();
                assignment.MarkInProgress();
                assignment.MarkCompleted();
            }

            assignments.Add(assignment);

            // Update job with assignment and status
            job.AssignContractor(selectedContractor.Id, startUtc, endUtc);
            
            // Update job status to match assignment status
            if (jobStatus == JobStatus.InProgress && job.Status == JobStatus.Assigned)
            {
                job.UpdateStatus(JobStatus.InProgress);
            }
            else if (jobStatus == JobStatus.Completed && job.Status != JobStatus.Completed)
            {
                // Transition through valid states
                if (job.Status == JobStatus.Created)
                {
                    job.UpdateStatus(JobStatus.Assigned);
                }
                if (job.Status == JobStatus.Assigned)
                {
                    job.UpdateStatus(JobStatus.InProgress);
                }
                job.UpdateStatus(JobStatus.Completed);
            }

            // Create audit record
            var requestPayload = new
            {
                jobId = job.Id,
                desiredDate = job.DesiredDate,
                serviceWindow = new { start = job.ServiceWindow.Start, end = job.ServiceWindow.End },
                maxResults = 10
            };

            var candidatesJson = candidates.Select(c => new
            {
                contractorId = c.contractorId,
                finalScore = c.score,
                perFactorScores = new
                {
                    availability = c.availabilityScore,
                    rating = c.ratingScore,
                    distance = c.distanceScore,
                    rotation = 0
                },
                rationale = c.rationale,
                wasSelected = c.contractorId == selectedContractor.Id
            }).ToList();

            var auditRecord = new AuditRecommendation(
                Guid.NewGuid(),
                job.Id,
                JsonSerializer.Serialize(requestPayload),
                JsonSerializer.Serialize(candidatesJson),
                configVersion,
                "system", // Selection actor
                selectedContractor.Id
            );

            auditRecords.Add(auditRecord);
        }

        return (assignments, auditRecords);
    }

    private List<(Guid contractorId, double score, double availabilityScore, double ratingScore, double distanceScore, string rationale)> 
        GenerateCandidateScores(Job job, List<Contractor> compatibleContractors)
    {
        var candidates = new List<(Guid contractorId, double score, double availabilityScore, double ratingScore, double distanceScore, string rationale)>();

        // Take up to 10 random compatible contractors
        var candidateContractors = compatibleContractors
            .OrderBy(_ => _random.Next())
            .Take(Math.Min(10, compatibleContractors.Count))
            .ToList();

        foreach (var contractor in candidateContractors)
        {
            // Generate realistic scores
            // High-rated contractors get better scores
            var ratingScore = contractor.Rating * 1.0; // 30-95

            // Distance score (inverse of distance, normalized 0-100)
            // For demo, we'll generate a random distance-based score
            var distanceScore = _random.Next(40, 100);

            // Availability score (random but weighted by rating)
            var availabilityScore = contractor.Rating > 70 
                ? _random.Next(70, 100) 
                : _random.Next(40, 90);

            // Final score (weighted average)
            var finalScore = (availabilityScore * 0.4 + ratingScore * 0.3 + distanceScore * 0.3);

            var rationale = $"Rating: {contractor.Rating}, Distance: {distanceScore:F0}, Available";

            candidates.Add((contractor.Id, finalScore, availabilityScore, ratingScore, distanceScore, rationale));
        }

        return candidates.OrderByDescending(c => c.score).ToList();
    }

    private Contractor SelectContractorWeighted(
        List<Contractor> compatibleContractors, 
        List<(Guid contractorId, double score, double availabilityScore, double ratingScore, double distanceScore, string rationale)> candidates)
    {
        // Weight selection by score (top candidates more likely)
        var topCandidate = candidates.First();
        
        // 70% chance to select top candidate, 30% chance for others (to show variety)
        if (_random.Next(100) < 70)
        {
            return compatibleContractors.First(c => c.Id == topCandidate.contractorId);
        }
        else
        {
            var otherCandidates = candidates.Skip(1).Take(3).ToList();
            if (otherCandidates.Any())
            {
                var selected = otherCandidates[_random.Next(otherCandidates.Count)];
                return compatibleContractors.First(c => c.Id == selected.contractorId);
            }
            return compatibleContractors.First(c => c.Id == topCandidate.contractorId);
        }
    }
}

