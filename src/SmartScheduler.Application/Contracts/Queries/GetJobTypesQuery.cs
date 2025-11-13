using MediatR;
using SmartScheduler.Application.Contracts.DTOs;

namespace SmartScheduler.Application.Contracts.Queries;

/// <summary>
/// Query to get all job types.
/// </summary>
public record GetJobTypesQuery : IRequest<JobTypesResponseDto>;

