using MediatR;
using SmartScheduler.Application.Contracts.Commands;
using SmartScheduler.Application.Contracts.DTOs;
using SmartScheduler.Application.Contracts.Mapping;
using SmartScheduler.Application.Contracts.Services;
using SmartScheduler.Domain.Contracts.Entities;
using SmartScheduler.Domain.Contracts.Repositories;
using SmartScheduler.Domain.Contracts.ValueObjects;

namespace SmartScheduler.Application.Contracts.Handlers;

/// <summary>
/// Handler for CreateJobCommand.
/// </summary>
public class CreateJobCommandHandler : IRequestHandler<CreateJobCommand, JobDto>
{
    private readonly IJobRepository _repository;
    private readonly IAddressValidationService _addressValidationService;
    private readonly ITimezoneService _timezoneService;

    public CreateJobCommandHandler(
        IJobRepository repository,
        IAddressValidationService addressValidationService,
        ITimezoneService timezoneService)
    {
        _repository = repository;
        _addressValidationService = addressValidationService;
        _timezoneService = timezoneService;
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

        return job.ToDto();
    }
}

