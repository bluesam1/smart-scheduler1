using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using SmartScheduler.Application.Contracts.DTOs;
using SmartScheduler.Application.Contracts.Services;
using SmartScheduler.Application.DTOs;
using SmartScheduler.Application.Recommendations.Configuration;
using SmartScheduler.Application.Recommendations.DTOs;
using SmartScheduler.Application.Recommendations.Queries;
using SmartScheduler.Application.Recommendations.Services;
using SmartScheduler.Domain.Contracts.Entities;
using SmartScheduler.Domain.Contracts.Repositories;
using SmartScheduler.Domain.Contracts.ValueObjects;
using SmartScheduler.Domain.Scheduling.Services;
using SmartScheduler.Domain.Scheduling.Utilities;
using SmartScheduler.Realtime.Services;

namespace SmartScheduler.Application.Recommendations.Handlers;

/// <summary>
/// Handler for GetRecommendationsQuery.
/// Generates ranked contractor recommendations for a job.
/// </summary>
public class GetRecommendationsQueryHandler : IRequestHandler<GetRecommendationsQuery, RecommendationResponse>
{
    private readonly IJobRepository _jobRepository;
    private readonly IContractorRepository _contractorRepository;
    private readonly IAuditRecommendationRepository _auditRepository;
    private readonly IAvailabilityEngine _availabilityEngine;
    private readonly ISlotGenerator _slotGenerator;
    private readonly IDistanceCalculationService _distanceService;
    private readonly IScoringService _scoringService;
    private readonly ITieBreakerService _tieBreakerService;
    private readonly IRationaleGenerator _rationaleGenerator;
    private readonly IRotationBoostService _rotationBoostService;
    private readonly IScoringWeightsConfigLoader _configLoader;
    private readonly IRealtimePublisher _realtimePublisher;
    private readonly ILogger<GetRecommendationsQueryHandler> _logger;

    public GetRecommendationsQueryHandler(
        IJobRepository jobRepository,
        IContractorRepository contractorRepository,
        IAuditRecommendationRepository auditRepository,
        IAvailabilityEngine availabilityEngine,
        ISlotGenerator slotGenerator,
        IDistanceCalculationService distanceService,
        IScoringService scoringService,
        ITieBreakerService tieBreakerService,
        IRationaleGenerator rationaleGenerator,
        IRotationBoostService rotationBoostService,
        IScoringWeightsConfigLoader configLoader,
        IRealtimePublisher realtimePublisher,
        ILogger<GetRecommendationsQueryHandler> logger)
    {
        _jobRepository = jobRepository;
        _contractorRepository = contractorRepository;
        _auditRepository = auditRepository;
        _availabilityEngine = availabilityEngine;
        _slotGenerator = slotGenerator;
        _distanceService = distanceService;
        _scoringService = scoringService;
        _tieBreakerService = tieBreakerService;
        _rationaleGenerator = rationaleGenerator;
        _rotationBoostService = rotationBoostService;
        _configLoader = configLoader;
        _realtimePublisher = realtimePublisher;
        _logger = logger;
    }

    public async Task<RecommendationResponse> Handle(
        GetRecommendationsQuery request,
        CancellationToken cancellationToken)
    {
        var requestId = Guid.NewGuid();
        var generatedAt = DateTime.UtcNow;

        // Get the job
        var job = await _jobRepository.GetByIdAsync(request.JobId, cancellationToken);
        if (job == null)
        {
            throw new ArgumentException($"Job with ID {request.JobId} not found.", nameof(request));
        }

        // Determine service window (use request override or job's service window)
        var serviceWindow = request.ServiceWindow != null
            ? new TimeWindow(request.ServiceWindow.Start, request.ServiceWindow.End)
            : job.ServiceWindow;

        // Get all contractors (filter by skills if job has required skills)
        var allContractors = job.RequiredSkills.Count > 0
            ? await _contractorRepository.GetBySkillsAsync(job.RequiredSkills, cancellationToken)
            : await _contractorRepository.GetAllAsync(cancellationToken);

        // Get all jobs to find existing assignments for contractors
        var allJobs = await _jobRepository.GetAllAsync(cancellationToken);

        // Build candidate list with scores
        var candidates = new List<CandidateData>();

        foreach (var contractor in allContractors)
        {
            // Check skills compatibility (hard requirement)
            if (!HasRequiredSkills(contractor, job.RequiredSkills))
            {
                continue;
            }

            // Get existing assignments for this contractor that overlap with service window
            var existingAssignments = GetExistingAssignments(contractor.Id, allJobs, serviceWindow);

            // Get available slots using availability engine
            var availableSlots = _availabilityEngine.CalculateAvailableSlots(
                contractor.WorkingHours.ToList(),
                serviceWindow,
                existingAssignments,
                job.Duration,
                contractor.Timezone,
                job.Timezone,
                contractor.Calendar);

            // Skip if no available slots
            if (availableSlots.Count == 0)
            {
                continue;
            }

            // Calculate distance using Haversine (coarse filtering)
            var haversineDistance = HaversineCalculator.CalculateDistance(
                contractor.BaseLocation.Latitude,
                contractor.BaseLocation.Longitude,
                job.Location.Latitude,
                job.Location.Longitude);

            // For now, use Haversine distance for all candidates
            // TODO: Implement coarse-to-refine: use ORS for top 5-8 candidates
            var distanceMeters = haversineDistance;
            var etaMinutes = (int)Math.Ceiling(haversineDistance / 1000.0 / 50.0 * 60.0); // Estimate: 50 km/h average

            // Calculate rotation boost
            var utilization = CalculateUtilization(contractor, allJobs, serviceWindow);
            var rotationBoost = _rotationBoostService.CalculateBoost(utilization);

            // Calculate score
            var scoreResult = _scoringService.CalculateScore(
                contractor.Rating,
                availableSlots,
                distanceMeters,
                rotationBoost);

            // Generate time slots using slot generator
            var generatedSlots = _slotGenerator.GenerateSlots(
                contractor.WorkingHours.ToList(),
                serviceWindow,
                existingAssignments,
                job.Duration,
                contractor.Timezone,
                job.Timezone,
                contractor.Calendar,
                etaMinutes,
                null, // previousJobToJobEtaMinutes - would need to find previous job
                contractor.Rating,
                job.Priority == Priority.Rush);

            // Generate rationale
            var rationale = _rationaleGenerator.GenerateRationale(
                scoreResult.Breakdown,
                scoreResult.FinalScore);

            candidates.Add(new CandidateData
            {
                Contractor = contractor,
                AvailableSlots = availableSlots,
                DistanceMeters = distanceMeters,
                EtaMinutes = etaMinutes,
                ScoreResult = scoreResult,
                GeneratedSlots = generatedSlots,
                Rationale = rationale,
                Utilization = utilization
            });
        }

        // Sort by score (descending)
        var sortedCandidates = candidates
            .OrderByDescending(c => c.ScoreResult.FinalScore)
            .ToList();

        // Apply tie-breakers for candidates with equal scores
        var finalRanking = ApplyTieBreakers(sortedCandidates, serviceWindow);

        // Take top N
        var topCandidates = finalRanking
            .Take(Math.Min(request.MaxResults, 50)) // Cap at 50
            .ToList();

        // Convert to DTOs
        var recommendations = topCandidates.Select(c => new RecommendationDto
        {
            ContractorId = c.Contractor.Id,
            ContractorName = c.Contractor.Name,
            Score = Math.Round(c.ScoreResult.FinalScore, 2),
            ScoreBreakdown = c.ScoreResult.Breakdown,
            Rationale = c.Rationale,
            SuggestedSlots = c.GeneratedSlots.Select(s => new TimeSlotDto
            {
                StartUtc = s.Window.Start,
                EndUtc = s.Window.End,
                Type = s.Type switch
                {
                    SlotType.Earliest => "earliest",
                    SlotType.LowestTravel => "lowest-travel",
                    SlotType.HighestConfidence => "highest-confidence",
                    _ => "earliest"
                },
                Confidence = s.Confidence
            }).ToList(),
            Distance = Math.Round(c.DistanceMeters, 0),
            Eta = c.EtaMinutes,
            ContractorBaseLocation = c.Contractor.BaseLocation.FormattedAddress 
                ?? c.Contractor.BaseLocation.Address 
                ?? $"{c.Contractor.BaseLocation.City}, {c.Contractor.BaseLocation.State}"
        }).ToList();

        // Get config version
        var configVersion = _configLoader.GetConfig().Version;

        // Persist audit trail (fire-and-forget, don't block response)
        _ = Task.Run(async () =>
        {
            try
            {
                await PersistAuditTrailAsync(
                    requestId,
                    job.Id,
                    request,
                    finalRanking,
                    configVersion,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to persist audit trail for recommendation request {RequestId}", requestId);
            }
        }, cancellationToken);

        // Publish RecommendationReady event only if explicitly requested (e.g., from /recalculate endpoint)
        // Regular fetches should not trigger events to avoid infinite loops
        if (request.PublishEvent)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    // MVP uses default region - can be enhanced to derive from job location or user context
                    const string region = "Default";
                    await _realtimePublisher.PublishRecommendationReadyAsync(
                        job.Id.ToString(),
                        requestId.ToString(),
                        region,
                        configVersion,
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to publish RecommendationReady event for job {JobId}", job.Id);
                }
            }, cancellationToken);
        }

        return new RecommendationResponse
        {
            RequestId = requestId,
            JobId = job.Id,
            Recommendations = recommendations,
            BestRecommendationContractorId = topCandidates.FirstOrDefault()?.Contractor.Id,
            ConfigVersion = configVersion,
            GeneratedAt = generatedAt
        };
    }

    /// <summary>
    /// Persists audit trail for recommendation request.
    /// </summary>
    private async Task PersistAuditTrailAsync(
        Guid requestId,
        Guid jobId,
        GetRecommendationsQuery request,
        IReadOnlyList<CandidateData> candidates,
        int configVersion,
        CancellationToken cancellationToken)
    {
        // Serialize request payload
        var requestPayload = new
        {
            jobId = request.JobId,
            desiredDate = request.DesiredDate.ToString("yyyy-MM-dd"),
            serviceWindow = request.ServiceWindow != null ? new
            {
                start = request.ServiceWindow.Start,
                end = request.ServiceWindow.End
            } : null,
            maxResults = request.MaxResults
        };
        var requestPayloadJson = JsonSerializer.Serialize(requestPayload);

        // Serialize candidates with scores
        var candidateScores = candidates.Select(c => new CandidateScore
        {
            ContractorId = c.Contractor.Id,
            FinalScore = c.ScoreResult.FinalScore,
            PerFactorScores = new ScoreBreakdownData
            {
                Availability = c.ScoreResult.Breakdown.Availability,
                Rating = c.ScoreResult.Breakdown.Rating,
                Distance = c.ScoreResult.Breakdown.Distance,
                Rotation = c.ScoreResult.Breakdown.Rotation
            },
            Rationale = c.Rationale,
            WasSelected = false // Will be updated when assignment is created
        }).ToList();
        var candidatesJson = JsonSerializer.Serialize(candidateScores);

        // Create audit record
        var auditRecord = new AuditRecommendation(
            requestId,
            jobId,
            requestPayloadJson,
            candidatesJson,
            configVersion,
            "system");

        await _auditRepository.AddAsync(auditRecord, cancellationToken);
    }

    /// <summary>
    /// Checks if contractor has all required skills.
    /// </summary>
    private static bool HasRequiredSkills(Contractor contractor, IReadOnlyList<string> requiredSkills)
    {
        if (requiredSkills.Count == 0)
            return true;

        var contractorSkills = contractor.Skills.Select(s => s.ToLowerInvariant()).ToHashSet();
        return requiredSkills.All(skill => contractorSkills.Contains(skill.ToLowerInvariant()));
    }

    /// <summary>
    /// Gets existing assignments for a contractor that overlap with the service window.
    /// </summary>
    private static IReadOnlyList<TimeWindow> GetExistingAssignments(
        Guid contractorId,
        IReadOnlyList<Job> allJobs,
        TimeWindow serviceWindow)
    {
        var assignments = new List<TimeWindow>();

        foreach (var job in allJobs)
        {
            foreach (var assignment in job.AssignedContractors)
            {
                if (assignment.ContractorId == contractorId)
                {
                    var assignmentWindow = new TimeWindow(assignment.StartUtc, assignment.EndUtc);
                    
                    // Check if assignment overlaps with service window
                    if (assignmentWindow.Start < serviceWindow.End && assignmentWindow.End > serviceWindow.Start)
                    {
                        assignments.Add(assignmentWindow);
                    }
                }
            }
        }

        return assignments;
    }

    /// <summary>
    /// Calculates contractor utilization (0.0-1.0) based on assigned time vs available time.
    /// </summary>
    private static double CalculateUtilization(
        Contractor contractor,
        IReadOnlyList<Job> allJobs,
        TimeWindow serviceWindow)
    {
        // Get all assignments for this contractor
        var assignments = GetExistingAssignments(contractor.Id, allJobs, serviceWindow);
        
        if (assignments.Count == 0)
            return 0.0;

        // Calculate total assigned minutes
        var assignedMinutes = assignments.Sum(a => a.DurationMinutes);

        // Calculate available minutes in service window
        var availableMinutes = serviceWindow.DurationMinutes;

        if (availableMinutes == 0)
            return 0.0;

        return Math.Min(1.0, assignedMinutes / (double)availableMinutes);
    }

    /// <summary>
    /// Applies tie-breaker logic to candidates with equal scores.
    /// </summary>
    private IReadOnlyList<CandidateData> ApplyTieBreakers(
        IReadOnlyList<CandidateData> candidates,
        TimeWindow serviceWindow)
    {
        if (candidates.Count <= 1)
            return candidates;

        // Group by score
        var groupedByScore = candidates
            .GroupBy(c => Math.Round(c.ScoreResult.FinalScore, 2))
            .ToList();

        var result = new List<CandidateData>();

        foreach (var group in groupedByScore)
        {
            if (group.Count() == 1)
            {
                result.Add(group.First());
                continue;
            }

            // Apply tie-breakers for this group
            var contractorIds = group.Select(c => c.Contractor.Id).ToList();
            var availableSlotsDict = group.ToDictionary(
                c => c.Contractor.Id,
                c => c.AvailableSlots);
            var utilizationDict = group.ToDictionary(
                c => c.Contractor.Id,
                c => c.Utilization);
            var travelMinutesDict = group.ToDictionary(
                c => c.Contractor.Id,
                c => (int?)c.EtaMinutes);

            var orderedIds = _tieBreakerService.ApplyTieBreakers(
                contractorIds,
                availableSlotsDict,
                utilizationDict,
                travelMinutesDict);

            // Add in tie-breaker order
            var orderedCandidates = orderedIds
                .Select(id => group.First(c => c.Contractor.Id == id))
                .ToList();

            result.AddRange(orderedCandidates);
        }

        return result;
    }

    /// <summary>
    /// Internal data structure for candidate processing.
    /// </summary>
    private class CandidateData
    {
        public Contractor Contractor { get; init; } = null!;
        public IReadOnlyList<TimeWindow> AvailableSlots { get; init; } = Array.Empty<TimeWindow>();
        public double DistanceMeters { get; init; }
        public int EtaMinutes { get; init; }
        public ScoreResult ScoreResult { get; init; } = null!;
        public IReadOnlyList<GeneratedSlot> GeneratedSlots { get; init; } = Array.Empty<GeneratedSlot>();
        public string Rationale { get; init; } = string.Empty;
        public double Utilization { get; init; }
    }
}

