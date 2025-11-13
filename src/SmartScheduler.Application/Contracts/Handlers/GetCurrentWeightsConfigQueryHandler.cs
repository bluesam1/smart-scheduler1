using MediatR;
using SmartScheduler.Application.Contracts.DTOs;
using SmartScheduler.Application.Contracts.Queries;
using SmartScheduler.Application.Recommendations.Configuration;
using SmartScheduler.Domain.Contracts.Repositories;
using System.Text.Json;

namespace SmartScheduler.Application.Contracts.Handlers;

/// <summary>
/// Handler for GetCurrentWeightsConfigQuery.
/// </summary>
public class GetCurrentWeightsConfigQueryHandler : IRequestHandler<GetCurrentWeightsConfigQuery, WeightsConfigResponseDto?>
{
    private readonly IWeightsConfigRepository _repository;
    private readonly IScoringWeightsConfigLoader _configLoader;

    public GetCurrentWeightsConfigQueryHandler(
        IWeightsConfigRepository repository,
        IScoringWeightsConfigLoader configLoader)
    {
        _repository = repository;
        _configLoader = configLoader;
    }

    public async Task<WeightsConfigResponseDto?> Handle(
        GetCurrentWeightsConfigQuery request,
        CancellationToken cancellationToken)
    {
        // First try to get from database
        var dbConfig = await _repository.GetActiveAsync(cancellationToken);
        if (dbConfig != null)
        {
            var config = JsonSerializer.Deserialize<ScoringWeightsConfig>(dbConfig.ConfigJson);
            if (config != null)
            {
                return new WeightsConfigResponseDto
                {
                    Version = dbConfig.Version,
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
                    ChangeNotes = dbConfig.ChangeNotes,
                    CreatedBy = dbConfig.CreatedBy,
                    CreatedAt = dbConfig.CreatedAt,
                };
            }
        }

        // Fallback to config loader (appsettings.json)
        var appConfig = _configLoader.GetConfig();
        return new WeightsConfigResponseDto
        {
            Version = appConfig.Version,
            Weights = new WeightFactorsDto
            {
                Availability = appConfig.Weights.Availability,
                Rating = appConfig.Weights.Rating,
                Distance = appConfig.Weights.Distance,
            },
            TieBreakers = appConfig.TieBreakers,
            Rotation = new RotationConfigDto
            {
                Enabled = appConfig.Rotation.Enabled,
                Boost = appConfig.Rotation.Boost,
                UnderUtilizationThreshold = appConfig.Rotation.UnderUtilizationThreshold,
            },
            ChangeNotes = "Default configuration from appsettings.json",
            CreatedBy = "System",
            CreatedAt = DateTime.UtcNow,
        };
    }
}

