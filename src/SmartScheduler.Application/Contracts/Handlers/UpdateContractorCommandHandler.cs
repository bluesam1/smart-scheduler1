using MediatR;
using SmartScheduler.Application.Contracts.Commands;
using SmartScheduler.Application.Contracts.DTOs;
using SmartScheduler.Application.Contracts.Mapping;
using SmartScheduler.Application.Contracts.Services;
using SmartScheduler.Domain.Contracts.Repositories;

namespace SmartScheduler.Application.Contracts.Handlers;

/// <summary>
/// Handler for UpdateContractorCommand.
/// </summary>
public class UpdateContractorCommandHandler : IRequestHandler<UpdateContractorCommand, ContractorDto>
{
    private readonly IContractorRepository _repository;
    private readonly ISkillNormalizationService _skillNormalizationService;

    public UpdateContractorCommandHandler(
        IContractorRepository repository,
        ISkillNormalizationService skillNormalizationService)
    {
        _repository = repository;
        _skillNormalizationService = skillNormalizationService;
    }

    public async Task<ContractorDto> Handle(
        UpdateContractorCommand request,
        CancellationToken cancellationToken)
    {
        var contractor = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (contractor == null)
        {
            throw new KeyNotFoundException($"Contractor with ID {request.Id} not found.");
        }

        var req = request.Request;

        // Update properties if provided
        if (!string.IsNullOrWhiteSpace(req.Name))
        {
            contractor.UpdateName(req.Name);
        }

        if (req.BaseLocation != null)
        {
            contractor.UpdateBaseLocation(req.BaseLocation.ToDomain());
        }

        if (req.WorkingHours != null && req.WorkingHours.Count > 0)
        {
            var workingHours = req.WorkingHours.Select(wh => wh.ToDomain()).ToList();
            contractor.UpdateWorkingHours(workingHours);
        }

        if (req.Skills != null)
        {
            // Normalize skills before updating
            var normalizedSkills = _skillNormalizationService.Normalize(req.Skills);
            
            // Validate skills against system configuration (when available)
            // TODO: Enable validation when SystemConfiguration is implemented (story 9.1)
            // var invalidSkills = await _skillNormalizationService.ValidateAgainstConfigurationAsync(normalizedSkills, cancellationToken);
            // if (invalidSkills.Count > 0)
            // {
            //     throw new ArgumentException($"Invalid skills: {string.Join(", ", invalidSkills)}");
            // }
            
            contractor.UpdateSkills(normalizedSkills);
        }

        if (req.Rating.HasValue)
        {
            contractor.UpdateRating(req.Rating.Value);
        }

        if (req.Calendar != null)
        {
            contractor.UpdateCalendar(req.Calendar.ToDomain());
        }

        if (req.MaxJobsPerDay.HasValue)
        {
            contractor.UpdateMaxJobsPerDay(req.MaxJobsPerDay.Value);
        }

        await _repository.UpdateAsync(contractor, cancellationToken);

        return contractor.ToDto();
    }
}

