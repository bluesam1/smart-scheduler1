using MediatR;
using SmartScheduler.Application.Contracts.DTOs;
using SmartScheduler.Application.Contracts.Mapping;
using SmartScheduler.Application.Contracts.Queries;
using SmartScheduler.Domain.Contracts.Repositories;
using SmartScheduler.Domain.Contracts.ValueObjects;

namespace SmartScheduler.Application.Contracts.Handlers;

/// <summary>
/// Handler for GetJobsQuery.
/// </summary>
public class GetJobsQueryHandler : IRequestHandler<GetJobsQuery, IReadOnlyList<JobDto>>
{
    private readonly IJobRepository _repository;

    public GetJobsQueryHandler(IJobRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<JobDto>> Handle(
        GetJobsQuery request,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<SmartScheduler.Domain.Contracts.Entities.Job> jobs;

        if (!string.IsNullOrWhiteSpace(request.Status) && Enum.TryParse<JobStatus>(request.Status, out var status))
        {
            jobs = await _repository.GetByStatusAsync(status, cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(request.Priority) && Enum.TryParse<Priority>(request.Priority, out var priority))
        {
            jobs = await _repository.GetByPriorityAsync(priority, cancellationToken);
        }
        else
        {
            jobs = await _repository.GetAllAsync(cancellationToken);
        }

        // Convert to DTOs without travel enrichment (too expensive for list view)
        // Travel time will be calculated only when viewing individual job details
        var result = jobs
            .Select(j => j.ToDto())
            .ToList();

        if (request.Limit.HasValue && request.Limit.Value > 0)
        {
            result = result.Take(request.Limit.Value).ToList();
        }

        return result;
    }

    // NOTE: Travel time enrichment removed from GetJobs to avoid performance issues
    // It was causing hundreds of API calls to OpenRouteService when loading the dashboard
    // Travel time is still calculated in GetJobById for individual job detail views
}

