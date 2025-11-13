using MediatR;
using SmartScheduler.Application.Contracts.DTOs;
using SmartScheduler.Application.Contracts.Mapping;
using SmartScheduler.Application.Contracts.Queries;
using SmartScheduler.Domain.Contracts.Repositories;

namespace SmartScheduler.Application.Contracts.Handlers;

/// <summary>
/// Handler for GetContractorsQuery.
/// </summary>
public class GetContractorsQueryHandler : IRequestHandler<GetContractorsQuery, IReadOnlyList<ContractorDto>>
{
    private readonly IContractorRepository _repository;

    public GetContractorsQueryHandler(IContractorRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<ContractorDto>> Handle(
        GetContractorsQuery request,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<SmartScheduler.Domain.Contracts.Entities.Contractor> contractors;

        if (request.Skills != null && request.Skills.Count > 0)
        {
            contractors = await _repository.GetBySkillsAsync(request.Skills, cancellationToken);
        }
        else
        {
            contractors = await _repository.GetAllAsync(cancellationToken);
        }

        var result = contractors
            .Select(c => c.ToDto())
            .ToList();

        if (request.Limit.HasValue && request.Limit.Value > 0)
        {
            result = result.Take(request.Limit.Value).ToList();
        }

        return result;
    }
}


