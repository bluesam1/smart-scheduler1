using MediatR;
using Microsoft.AspNetCore.Mvc;
using SmartScheduler.Application.Contracts.Queries;
using SmartScheduler.Application.Recommendations.DTOs;
using SmartScheduler.Application.Recommendations.Queries;
using SmartScheduler.Domain.Contracts.Repositories;
using SmartScheduler.Realtime.Services;

namespace SmartScheduler.Api.Endpoints.Recommendations;

/// <summary>
/// Recommendations API endpoints.
/// </summary>
public static class RecommendationEndpoints
{
    public static void MapRecommendationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/recommendations")
            .RequireAuthorization("Dispatcher")
            .WithTags("Recommendations")
            .WithOpenApi();

        // POST /api/recommendations - Get contractor recommendations for a job
        group.MapPost("/", async (
            IMediator mediator,
            [FromBody] RecommendationRequest request,
            CancellationToken cancellationToken) =>
        {
            try
            {
                // Validate request
                if (request.JobId == Guid.Empty)
                {
                    return Results.BadRequest(new { message = "JobId is required." });
                }

                if (request.MaxResults < 1 || request.MaxResults > 50)
                {
                    return Results.BadRequest(new { message = "MaxResults must be between 1 and 50." });
                }

                var query = new GetRecommendationsQuery
                {
                    JobId = request.JobId,
                    DesiredDate = request.DesiredDate,
                    ServiceWindow = request.ServiceWindow,
                    MaxResults = request.MaxResults,
                    PublishEvent = false // Regular fetch should not trigger events
                };

                var response = await mediator.Send(query, cancellationToken);
                return Results.Ok(response);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("GetRecommendations")
        .WithSummary("Get contractor recommendations")
        .WithDescription("Get ranked contractor recommendations for a job with up to 3 suggested time slots per contractor.")
        .Produces<RecommendationResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status500InternalServerError);

        // POST /api/recommendations/recalculate - Recalculate recommendations for a job
        group.MapPost("/recalculate", async (
            IMediator mediator,
            IJobRepository jobRepository,
            IRealtimePublisher realtimePublisher,
            [FromBody] RecalculateRequest request,
            CancellationToken cancellationToken) =>
        {
            try
            {
                // Validate request
                if (request.JobId == Guid.Empty)
                {
                    return Results.BadRequest(new { message = "JobId is required." });
                }

                // Get the job
                var jobQuery = new GetJobByIdQuery { Id = request.JobId };
                var job = await mediator.Send(jobQuery, cancellationToken);

                if (job == null)
                {
                    return Results.NotFound(new { message = $"Job with ID {request.JobId} not found." });
                }

                // Trigger recommendation calculation with event publishing enabled
                var recommendationsQuery = new GetRecommendationsQuery
                {
                    JobId = request.JobId,
                    DesiredDate = DateOnly.FromDateTime(job.DesiredDate),
                    ServiceWindow = job.ServiceWindow != null ? new TimeWindowDto
                    {
                        Start = job.ServiceWindow.Start,
                        End = job.ServiceWindow.End
                    } : null,
                    MaxResults = 10,
                    PublishEvent = true // Explicit recalculation should publish event
                };

                var response = await mediator.Send(recommendationsQuery, cancellationToken);

                // Update job's LastRecommendationAuditId
                var jobEntity = await jobRepository.GetByIdAsync(request.JobId, cancellationToken);
                if (jobEntity != null)
                {
                    jobEntity.UpdateLastRecommendationAuditId(response.RequestId);
                    await jobRepository.UpdateAsync(jobEntity, cancellationToken);
                }

                // Note: RecommendationReady event is already published by GetRecommendationsQueryHandler
                // No need to publish again here to avoid duplicate events

                return Results.Ok(response);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("RecalculateRecommendations")
        .WithSummary("Recalculate recommendations for a job")
        .WithDescription("Recalculates contractor recommendations for a job, updates the job's recommendation reference, and publishes a real-time update event.")
        .Produces<RecommendationResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status500InternalServerError);
    }
}

/// <summary>
/// Request to recalculate recommendations for a job.
/// </summary>
public record RecalculateRequest
{
    public Guid JobId { get; init; }
}

