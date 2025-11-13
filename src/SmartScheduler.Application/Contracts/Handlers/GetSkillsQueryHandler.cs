using MediatR;
using SmartScheduler.Application.Contracts.DTOs;
using SmartScheduler.Application.Contracts.Queries;
using SmartScheduler.Domain.Contracts.Entities;
using SmartScheduler.Domain.Contracts.Repositories;

namespace SmartScheduler.Application.Contracts.Handlers;

/// <summary>
/// Handler for GetSkillsQuery.
/// </summary>
public class GetSkillsQueryHandler : IRequestHandler<GetSkillsQuery, SkillsResponseDto>
{
    private readonly ISystemConfigurationRepository _repository;

    public GetSkillsQueryHandler(ISystemConfigurationRepository repository)
    {
        _repository = repository;
    }

    public async Task<SkillsResponseDto> Handle(
        GetSkillsQuery request,
        CancellationToken cancellationToken)
    {
        var config = await _repository.GetByTypeAsync(ConfigurationType.Skills, cancellationToken);
        
        if (config == null)
        {
            // Return empty list if no configuration exists yet
            return new SkillsResponseDto { Skills = Array.Empty<string>() };
        }

        return new SkillsResponseDto { Skills = config.ValuesReadOnly };
    }
}

