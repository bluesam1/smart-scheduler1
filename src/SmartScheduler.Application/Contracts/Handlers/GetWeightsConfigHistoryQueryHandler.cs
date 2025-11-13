using MediatR;
using SmartScheduler.Application.Contracts.DTOs;
using SmartScheduler.Application.Contracts.Queries;
using SmartScheduler.Domain.Contracts.Repositories;

namespace SmartScheduler.Application.Contracts.Handlers;

/// <summary>
/// Handler for GetWeightsConfigHistoryQuery.
/// </summary>
public class GetWeightsConfigHistoryQueryHandler : IRequestHandler<GetWeightsConfigHistoryQuery, IReadOnlyList<WeightsConfigHistoryItemDto>>
{
    private readonly IWeightsConfigRepository _repository;

    public GetWeightsConfigHistoryQueryHandler(IWeightsConfigRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<WeightsConfigHistoryItemDto>> Handle(
        GetWeightsConfigHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var configs = await _repository.GetAllAsync(cancellationToken);
        
        return configs.Select(c => new WeightsConfigHistoryItemDto
        {
            Version = c.Version,
            IsActive = c.IsActive,
            ChangeNotes = c.ChangeNotes,
            CreatedBy = c.CreatedBy,
            CreatedAt = c.CreatedAt,
        }).ToList();
    }
}


