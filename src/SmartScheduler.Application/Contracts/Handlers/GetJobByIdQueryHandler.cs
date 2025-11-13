using MediatR;
using Microsoft.Extensions.Logging;
using SmartScheduler.Application.Contracts.DTOs;
using SmartScheduler.Application.Contracts.Mapping;
using SmartScheduler.Application.Contracts.Queries;
using SmartScheduler.Application.Contracts.Services;
using SmartScheduler.Domain.Contracts.Repositories;

namespace SmartScheduler.Application.Contracts.Handlers;

/// <summary>
/// Handler for GetJobByIdQuery.
/// </summary>
public class GetJobByIdQueryHandler : IRequestHandler<GetJobByIdQuery, JobDto?>
{
    private readonly IJobRepository _repository;
    private readonly IContractorRepository _contractorRepository;
    private readonly IDistanceCalculationService _distanceService;
    private readonly ILogger<GetJobByIdQueryHandler> _logger;

    public GetJobByIdQueryHandler(
        IJobRepository repository,
        IContractorRepository contractorRepository,
        IDistanceCalculationService distanceService,
        ILogger<GetJobByIdQueryHandler> logger)
    {
        _repository = repository;
        _contractorRepository = contractorRepository;
        _distanceService = distanceService;
        _logger = logger;
    }

    public async Task<JobDto?> Handle(
        GetJobByIdQuery request,
        CancellationToken cancellationToken)
    {
        var job = await _repository.GetByIdAsync(request.Id, cancellationToken);
        
        if (job == null)
        {
            return null;
        }

        return await EnrichJobDtoWithTravelInfo(job, cancellationToken);
    }

    private async Task<JobDto> EnrichJobDtoWithTravelInfo(
        SmartScheduler.Domain.Contracts.Entities.Job job,
        CancellationToken cancellationToken)
    {
        var baseDto = job.ToDto();
        
        // If no assigned contractors, return as-is
        if (job.AssignedContractors.Count == 0)
        {
            return baseDto;
        }

        // Enrich with travel information
        var enrichedAssignments = new List<ContractorAssignmentDto>();
        
        foreach (var assignment in job.AssignedContractors)
        {
            var enrichedAssignment = await EnrichAssignmentWithTravelInfo(
                assignment.ContractorId,
                job.Location.Latitude,
                job.Location.Longitude,
                assignment.StartUtc,
                assignment.EndUtc,
                cancellationToken);
            
            enrichedAssignments.Add(enrichedAssignment);
        }

        return baseDto with { AssignedContractors = enrichedAssignments };
    }

    private async Task<ContractorAssignmentDto> EnrichAssignmentWithTravelInfo(
        Guid contractorId,
        double jobLat,
        double jobLng,
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get contractor to find their base location
            var contractor = await _contractorRepository.GetByIdAsync(contractorId, cancellationToken);
            if (contractor == null)
            {
                return new ContractorAssignmentDto
                {
                    ContractorId = contractorId,
                    StartUtc = startUtc,
                    EndUtc = endUtc,
                    DistanceMeters = null,
                    TravelTimeMinutes = null
                };
            }

            // Calculate distance and ETA
            var result = await _distanceService.CalculateDistanceAsync(
                contractor.BaseLocation.Latitude,
                contractor.BaseLocation.Longitude,
                jobLat,
                jobLng,
                cancellationToken);

            // Calculate ETA from distance (assuming average speed)
            var etaMinutes = result.DistanceMeters.HasValue && result.DistanceMeters > 0 
                ? (int)Math.Round(result.DistanceMeters.Value / 1000.0 / 50.0 * 60.0) // 50 km/h average
                : 0;

            return new ContractorAssignmentDto
            {
                ContractorId = contractorId,
                StartUtc = startUtc,
                EndUtc = endUtc,
                DistanceMeters = result.DistanceMeters,
                TravelTimeMinutes = etaMinutes
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to calculate travel info for contractor {ContractorId}", contractorId);
            return new ContractorAssignmentDto
            {
                ContractorId = contractorId,
                StartUtc = startUtc,
                EndUtc = endUtc,
                DistanceMeters = null,
                TravelTimeMinutes = null
            };
        }
    }
}

