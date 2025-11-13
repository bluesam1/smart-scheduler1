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
/// Handler for CreateContractorCommand.
/// </summary>
public class CreateContractorCommandHandler : IRequestHandler<CreateContractorCommand, ContractorDto>
{
    private readonly IContractorRepository _repository;
    private readonly ISkillNormalizationService _skillNormalizationService;

    public CreateContractorCommandHandler(
        IContractorRepository repository,
        ISkillNormalizationService skillNormalizationService)
    {
        _repository = repository;
        _skillNormalizationService = skillNormalizationService;
    }

    public async Task<ContractorDto> Handle(
        CreateContractorCommand request,
        CancellationToken cancellationToken)
    {
        var req = request.Request;

        // Map DTOs to domain objects
        var baseLocation = req.BaseLocation.ToDomain();
        var workingHours = req.WorkingHours.Select(wh => wh.ToDomain()).ToList();
        var calendar = req.Calendar?.ToDomain();

        // Normalize skills
        var normalizedSkills = _skillNormalizationService.Normalize(req.Skills ?? Array.Empty<string>());

        // Validate skills against system configuration (when available)
        // TODO: Enable validation when SystemConfiguration is implemented (story 9.1)
        // var invalidSkills = await _skillNormalizationService.ValidateAgainstConfigurationAsync(normalizedSkills, cancellationToken);
        // if (invalidSkills.Count > 0)
        // {
        //     throw new ArgumentException($"Invalid skills: {string.Join(", ", invalidSkills)}");
        // }

        // Create domain entity
        var contractor = new Contractor(
            id: Guid.NewGuid(),
            name: req.Name,
            baseLocation: baseLocation,
            workingHours: workingHours,
            skills: normalizedSkills,
            rating: req.Rating ?? 50,
            calendar: calendar,
            maxJobsPerDay: req.MaxJobsPerDay ?? 4);

        await _repository.AddAsync(contractor, cancellationToken);

        return contractor.ToDto();
    }
}

