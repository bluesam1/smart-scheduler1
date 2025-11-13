using MediatR;
using Microsoft.AspNetCore.Mvc;
using SmartScheduler.Application.Recommendations.DTOs;
using SmartScheduler.Application.Recommendations.Queries;

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
                    MaxResults = request.MaxResults
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
    }
}

