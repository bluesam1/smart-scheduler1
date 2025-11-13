using MediatR;
using SmartScheduler.Application.Contracts.Commands;
using SmartScheduler.Application.Contracts.DTOs;
using SmartScheduler.Application.Recommendations.Configuration;
using SmartScheduler.Domain.Contracts.Entities;
using SmartScheduler.Domain.Contracts.Repositories;
using System.Text.Json;

namespace SmartScheduler.Application.Contracts.Handlers;

/// <summary>
/// Handler for RollbackWeightsConfigCommand.
/// </summary>
public class RollbackWeightsConfigCommandHandler : IRequestHandler<RollbackWeightsConfigCommand, WeightsConfigResponseDto>
{
    private readonly IWeightsConfigRepository _repository;

    public RollbackWeightsConfigCommandHandler(IWeightsConfigRepository repository)
    {
        _repository = repository;
    }

    public async Task<WeightsConfigResponseDto> Handle(
        RollbackWeightsConfigCommand request,
        CancellationToken cancellationToken)
    {
        // Get the version to rollback to
        var targetConfig = await _repository.GetByVersionAsync(request.Request.Version, cancellationToken);
        if (targetConfig == null)
        {
            throw new KeyNotFoundException($"Weights configuration version {request.Request.Version} not found.");
        }

        // Deserialize the config
        var config = JsonSerializer.Deserialize<ScoringWeightsConfig>(targetConfig.ConfigJson);
        if (config == null)
        {
            throw new InvalidOperationException($"Failed to deserialize weights configuration version {request.Request.Version}.");
        }

        // Get next version number atomically to prevent race conditions
        // The repository method uses a transaction to ensure atomicity
        var newVersion = await _repository.GetNextVersionAsync(cancellationToken);

        // Create new config with incremented version
        config.Version = newVersion;
        var configJson = JsonSerializer.Serialize(config);

        // Deactivate all existing configs
        await _repository.DeactivateAllAsync(cancellationToken);

        // Create new config entity (rollback creates a new version)
        var entity = new WeightsConfig(
            id: Guid.NewGuid(),
            version: newVersion,
            configJson: configJson,
            changeNotes: $"Rollback to version {request.Request.Version}. {request.Request.ChangeNotes}".Trim(),
            createdBy: request.CreatedBy,
            isActive: true);

        // AddAsync may retry and return entity with different version if race condition occurred
        var savedEntity = await _repository.AddAsync(entity, cancellationToken);

        return new WeightsConfigResponseDto
        {
            Version = savedEntity.Version,
            Weights = new WeightFactorsDto
            {
                Availability = config.Weights.Availability,
                Rating = config.Weights.Rating,
                Distance = config.Weights.Distance,
            },
            TieBreakers = config.TieBreakers,
            Rotation = new RotationConfigDto
            {
                Enabled = config.Rotation.Enabled,
                Boost = config.Rotation.Boost,
                UnderUtilizationThreshold = config.Rotation.UnderUtilizationThreshold,
            },
            ChangeNotes = savedEntity.ChangeNotes,
            CreatedBy = savedEntity.CreatedBy,
            CreatedAt = savedEntity.CreatedAt,
        };
    }
}

