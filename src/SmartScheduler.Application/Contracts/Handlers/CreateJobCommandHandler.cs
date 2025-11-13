using MediatR;
using SmartScheduler.Application.Contracts.Commands;
using SmartScheduler.Application.Contracts.DTOs;
using SmartScheduler.Application.Contracts.Mapping;
using SmartScheduler.Application.Contracts.Services;
using SmartScheduler.Application.Recommendations.Queries;
using SmartScheduler.Domain.Contracts.Entities;
using SmartScheduler.Domain.Contracts.Repositories;
using SmartScheduler.Domain.Contracts.ValueObjects;
using SmartScheduler.Realtime.Services;

namespace SmartScheduler.Application.Contracts.Handlers;

/// <summary>
/// Handler for CreateJobCommand.
/// </summary>
public class CreateJobCommandHandler : IRequestHandler<CreateJobCommand, JobDto>
{
    private readonly IJobRepository _repository;
    private readonly IAddressValidationService _addressValidationService;
    private readonly ITimezoneService _timezoneService;
    private readonly IMediator _mediator;
    private readonly IRealtimePublisher _realtimePublisher;

    public CreateJobCommandHandler(
        IJobRepository repository,
        IAddressValidationService addressValidationService,
        ITimezoneService timezoneService,
        IMediator mediator,
        IRealtimePublisher realtimePublisher)
    {
        _repository = repository;
        _addressValidationService = addressValidationService;
        _timezoneService = timezoneService;
        _mediator = mediator;
        _realtimePublisher = realtimePublisher;
    }

    public async Task<JobDto> Handle(
        CreateJobCommand request,
        CancellationToken cancellationToken)
    {
        var req = request.Request;

        // Validate and enrich address using Google Places API
        var location = req.Location.ToDomain();
        var validatedLocation = await _addressValidationService.ValidateAndEnrichAddressAsync(
            location,
            req.PlaceId,
            cancellationToken);

        // Get timezone from coordinates
        var timezone = await _timezoneService.GetTimezoneAsync(
            validatedLocation.Latitude,
            validatedLocation.Longitude,
            cancellationToken);

        // Map DTOs to domain objects
        var serviceWindow = req.ServiceWindow.ToDomain();

        // Parse priority
        if (!Enum.TryParse<Priority>(req.Priority, out var priority))
        {
            priority = Priority.Normal;
        }

        // Create job entity
        var job = new Job(
            Guid.NewGuid(),
            req.Type,
            req.Duration,
            validatedLocation,
            timezone,
            serviceWindow,
            priority,
            req.DesiredDate,
            req.RequiredSkills,
            req.Description,
            req.AccessNotes,
            req.Tools?.ToList());

        // Save to database
        await _repository.AddAsync(job, cancellationToken);

        // Auto-trigger recommendations for the newly created job
        try
        {
            var recommendationsQuery = new GetRecommendationsQuery
            {
                JobId = job.Id,
                DesiredDate = DateOnly.FromDateTime(job.DesiredDate),
                ServiceWindow = new Recommendations.DTOs.TimeWindowDto
                {
                    Start = job.ServiceWindow.Start,
                    End = job.ServiceWindow.End
                },
                MaxResults = 10,
                PublishEvent = true // New job creation should notify dispatchers
            };

            var recommendationsResponse = await _mediator.Send(recommendationsQuery, cancellationToken);

            // Update job with the recommendation audit ID
            job.UpdateLastRecommendationAuditId(recommendationsResponse.RequestId);
            await _repository.UpdateAsync(job, cancellationToken);

            // Publish RecommendationReady event via SignalR
            const string region = "Default"; // MVP uses default region
            await _realtimePublisher.PublishRecommendationReadyAsync(
                job.Id.ToString(),
                recommendationsResponse.RequestId.ToString(),
                region,
                recommendationsResponse.ConfigVersion,
                cancellationToken);
        }
        catch (Exception)
        {
            // Log but don't fail job creation if recommendations fail
            // The recommendations can be calculated later via the recalculate endpoint
        }

        return job.ToDto();
    }
}

