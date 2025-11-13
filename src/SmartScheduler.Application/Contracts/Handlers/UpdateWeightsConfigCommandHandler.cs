using MediatR;
using SmartScheduler.Application.Contracts.Commands;
using SmartScheduler.Application.Contracts.DTOs;
using SmartScheduler.Application.Recommendations.Configuration;
using SmartScheduler.Domain.Contracts.Entities;
using SmartScheduler.Domain.Contracts.Repositories;
using System.Text.Json;

namespace SmartScheduler.Application.Contracts.Handlers;

/// <summary>
/// Handler for UpdateWeightsConfigCommand.
/// </summary>
public class UpdateWeightsConfigCommandHandler : IRequestHandler<UpdateWeightsConfigCommand, WeightsConfigResponseDto>
{
    private readonly IWeightsConfigRepository _repository;

    public UpdateWeightsConfigCommandHandler(IWeightsConfigRepository repository)
    {
        _repository = repository;
    }

    public async Task<WeightsConfigResponseDto> Handle(
        UpdateWeightsConfigCommand request,
        CancellationToken cancellationToken)
    {
        // Validate weights
        var weights = request.Request.Weights;
        if (weights.Availability < 0.0 || weights.Availability > 1.0)
            throw new ArgumentException("Availability weight must be between 0.0 and 1.0");
        if (weights.Rating < 0.0 || weights.Rating > 1.0)
            throw new ArgumentException("Rating weight must be between 0.0 and 1.0");
        if (weights.Distance < 0.0 || weights.Distance > 1.0)
            throw new ArgumentException("Distance weight must be between 0.0 and 1.0");

        // Validate rotation
        var rotation = request.Request.Rotation;
        if (rotation.Enabled)
        {
            if (rotation.Boost < 0.0 || rotation.Boost > 20.0)
                throw new ArgumentException("Rotation boost must be between 0.0 and 20.0");
            if (rotation.UnderUtilizationThreshold < 0.0 || rotation.UnderUtilizationThreshold > 1.0)
                throw new ArgumentException("UnderUtilizationThreshold must be between 0.0 and 1.0");
        }

        // Get next version number atomically to prevent race conditions
        // The repository method uses a transaction to ensure atomicity
        var newVersion = await _repository.GetNextVersionAsync(cancellationToken);

        // Create config object
        var config = new ScoringWeightsConfig
        {
            Version = newVersion,
            Weights = new WeightFactors
            {
                Availability = weights.Availability,
                Rating = weights.Rating,
                Distance = weights.Distance,
            },
            TieBreakers = request.Request.TieBreakers,
            Rotation = new RotationConfig
            {
                Enabled = rotation.Enabled,
                Boost = rotation.Boost,
                UnderUtilizationThreshold = rotation.UnderUtilizationThreshold,
            },
        };

        // Serialize to JSON
        var configJson = JsonSerializer.Serialize(config);

        // Deactivate all existing configs
        await _repository.DeactivateAllAsync(cancellationToken);

        // Create new config entity
        var entity = new WeightsConfig(
            id: Guid.NewGuid(),
            version: newVersion,
            configJson: configJson,
            changeNotes: request.Request.ChangeNotes,
            createdBy: request.CreatedBy,
            isActive: true);

        // AddAsync may retry and return entity with different version if race condition occurred
        var savedEntity = await _repository.AddAsync(entity, cancellationToken);

        return new WeightsConfigResponseDto
        {
            Version = savedEntity.Version,
            Weights = weights,
            TieBreakers = request.Request.TieBreakers,
            Rotation = rotation,
            ChangeNotes = savedEntity.ChangeNotes,
            CreatedBy = savedEntity.CreatedBy,
            CreatedAt = savedEntity.CreatedAt,
        };
    }
}

