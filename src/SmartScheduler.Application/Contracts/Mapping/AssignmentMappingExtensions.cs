using SmartScheduler.Application.Contracts.DTOs;
using SmartScheduler.Domain.Contracts.Entities;
using SmartScheduler.Domain.Contracts.ValueObjects;

namespace SmartScheduler.Application.Contracts.Mapping;

/// <summary>
/// Extension methods for mapping between Assignment domain entities and DTOs.
/// </summary>
public static class AssignmentMappingExtensions
{
    public static AssignmentDto ToDto(this Assignment assignment)
    {
        return new AssignmentDto
        {
            Id = assignment.Id,
            JobId = assignment.JobId,
            ContractorId = assignment.ContractorId,
            StartUtc = assignment.StartUtc,
            EndUtc = assignment.EndUtc,
            Source = assignment.Source.ToString(),
            AuditId = assignment.AuditId,
            Status = assignment.Status.ToString(),
            CreatedAt = assignment.CreatedAt,
            UpdatedAt = assignment.UpdatedAt
        };
    }
}

