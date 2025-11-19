using MediatR;
using SmartScheduler.Application.Contracts.Commands;
using SmartScheduler.Application.Contracts.DTOs;
using SmartScheduler.Application.Contracts.Mapping;
using SmartScheduler.Application.Contracts.Services;
using SmartScheduler.Domain.Contracts.Repositories;
using SmartScheduler.Domain.Contracts.ValueObjects;

namespace SmartScheduler.Application.Contracts.Handlers;

/// <summary>
/// Handler for UpdateJobCommand.
/// </summary>
public class UpdateJobCommandHandler : IRequestHandler<UpdateJobCommand, JobDto>
{
    private readonly IJobRepository _repository;
    private readonly IAddressValidationService _addressValidationService;
    private readonly ITimezoneService _timezoneService;

    public UpdateJobCommandHandler(
        IJobRepository repository,
        IAddressValidationService addressValidationService,
        ITimezoneService timezoneService)
    {
        _repository = repository;
        _addressValidationService = addressValidationService;
        _timezoneService = timezoneService;
    }

    public async Task<JobDto> Handle(
        UpdateJobCommand request,
        CancellationToken cancellationToken)
    {
        var job = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (job == null)
        {
            throw new KeyNotFoundException($"Job with ID {request.Id} not found.");
        }

        var req = request.Request;

        // Update properties if provided
        if (!string.IsNullOrWhiteSpace(req.Type))
        {
            job.UpdateType(req.Type);
        }

        if (req.Description != null)
        {
            job.UpdateDescription(req.Description);
        }

        if (req.Duration.HasValue)
        {
            job.UpdateDuration(req.Duration.Value);
        }

        if (req.Location != null)
        {
            // Validate and enrich address
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

            job.UpdateLocation(validatedLocation, timezone);
        }

        if (req.ServiceWindow != null)
        {
            var serviceWindow = req.ServiceWindow.ToDomain();
            job.Reschedule(serviceWindow);
        }

        if (!string.IsNullOrWhiteSpace(req.Priority) && Enum.TryParse<Priority>(req.Priority, out var priority))
        {
            job.UpdatePriority(priority);
        }

        if (req.RequiredSkills != null)
        {
            job.UpdateRequiredSkills(req.RequiredSkills);
        }

        if (req.AccessNotes != null)
        {
            job.UpdateAccessNotes(req.AccessNotes);
        }

        if (req.Tools != null)
        {
            job.UpdateTools(req.Tools.ToList());
        }

        if (req.DesiredDate.HasValue)
        {
            job.UpdateDesiredDate(req.DesiredDate.Value);
        }

        // Save changes
        await _repository.UpdateAsync(job, cancellationToken);

        return job.ToDto();
    }
}




