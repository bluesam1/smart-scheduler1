using SmartScheduler.Application.Contracts.DTOs;
using SmartScheduler.Domain.Contracts.Entities;
using SmartScheduler.Domain.Contracts.ValueObjects;

namespace SmartScheduler.Application.Contracts.Mapping;

/// <summary>
/// Extension methods for mapping between Job domain entities and DTOs.
/// </summary>
public static class JobMappingExtensions
{
    public static JobDto ToDto(this Job job)
    {
        return new JobDto
        {
            Id = job.Id,
            Type = job.Type,
            Description = job.Description,
            Duration = job.Duration,
            Location = job.Location.ToDto(),
            Timezone = job.Timezone,
            RequiredSkills = job.RequiredSkills.ToList(),
            ServiceWindow = job.ServiceWindow.ToDto(),
            Priority = job.Priority.ToString(),
            Status = job.Status.ToString(),
            AssignmentStatus = job.AssignmentStatus.ToString(),
            AssignedContractors = job.AssignedContractors.Select(a => a.ToDto()).ToList(),
            AccessNotes = job.AccessNotes,
            Tools = job.Tools,
            DesiredDate = job.DesiredDate,
            CreatedAt = job.CreatedAt,
            UpdatedAt = job.UpdatedAt,
            LastRecommendationAuditId = job.LastRecommendationAuditId
        };
    }

    public static TimeWindowDto ToDto(this TimeWindow timeWindow)
    {
        return new TimeWindowDto
        {
            Start = timeWindow.Start,
            End = timeWindow.End
        };
    }

    public static ContractorAssignmentDto ToDto(this ContractorAssignment assignment)
    {
        return new ContractorAssignmentDto
        {
            ContractorId = assignment.ContractorId,
            StartUtc = assignment.StartUtc,
            EndUtc = assignment.EndUtc
        };
    }

    public static TimeWindow ToDomain(this TimeWindowDto dto)
    {
        return new TimeWindow(dto.Start, dto.End);
    }
}

