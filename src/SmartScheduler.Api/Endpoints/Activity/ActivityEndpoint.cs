using MediatR;
using Microsoft.AspNetCore.Mvc;
using SmartScheduler.Application.Contracts.DTOs;
using SmartScheduler.Application.Contracts.Queries;

namespace SmartScheduler.Api.Endpoints.Activity;

/// <summary>
/// Activity feed API endpoints.
/// </summary>
public static class ActivityEndpoint
{
    public static void MapActivityEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/activity")
            .RequireAuthorization("Dispatcher")
            .WithTags("Activity")
            .WithOpenApi();

        // GET /api/activity - Get recent activities
        group.MapGet("/", async (
            IMediator mediator,
            [FromQuery] string? types, // Comma-separated list of activity types
            [FromQuery] int? limit,
            CancellationToken cancellationToken) =>
        {
            var activityTypes = !string.IsNullOrWhiteSpace(types)
                ? types.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList()
                : null;

            var query = new GetActivitiesQuery
            {
                Types = activityTypes,
                Limit = limit ?? 20
            };

            var activities = await mediator.Send(query, cancellationToken);
            return Results.Ok(activities);
        })
        .WithName("GetActivities")
        .WithSummary("Get recent activities")
        .WithDescription("Retrieves recent system activities from the event log. Supports filtering by activity types and limiting results.")
        .Produces<IReadOnlyList<ActivityDto>>(StatusCodes.Status200OK);
    }
}


