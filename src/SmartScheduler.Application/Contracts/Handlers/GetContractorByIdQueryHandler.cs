using MediatR;
using SmartScheduler.Application.Contracts.DTOs;
using SmartScheduler.Application.Contracts.Mapping;
using SmartScheduler.Application.Contracts.Queries;
using SmartScheduler.Domain.Contracts.Repositories;

namespace SmartScheduler.Application.Contracts.Handlers;

/// <summary>
/// Handler for GetContractorByIdQuery.
/// </summary>
public class GetContractorByIdQueryHandler : IRequestHandler<GetContractorByIdQuery, ContractorDto?>
{
    private readonly IContractorRepository _repository;

    public GetContractorByIdQueryHandler(IContractorRepository repository)
    {
        _repository = repository;
    }

    public async Task<ContractorDto?> Handle(
        GetContractorByIdQuery request,
        CancellationToken cancellationToken)
    {
        var contractor = await _repository.GetByIdAsync(request.Id, cancellationToken);
        return contractor?.ToDto();
    }
}




