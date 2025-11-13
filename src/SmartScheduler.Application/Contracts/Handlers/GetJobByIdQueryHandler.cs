using MediatR;
using SmartScheduler.Application.Contracts.DTOs;
using SmartScheduler.Application.Contracts.Mapping;
using SmartScheduler.Application.Contracts.Queries;
using SmartScheduler.Domain.Contracts.Repositories;

namespace SmartScheduler.Application.Contracts.Handlers;

/// <summary>
/// Handler for GetJobByIdQuery.
/// </summary>
public class GetJobByIdQueryHandler : IRequestHandler<GetJobByIdQuery, JobDto?>
{
    private readonly IJobRepository _repository;

    public GetJobByIdQueryHandler(IJobRepository repository)
    {
        _repository = repository;
    }

    public async Task<JobDto?> Handle(
        GetJobByIdQuery request,
        CancellationToken cancellationToken)
    {
        var job = await _repository.GetByIdAsync(request.Id, cancellationToken);
        return job?.ToDto();
    }
}

