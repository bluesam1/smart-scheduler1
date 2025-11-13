using MediatR;
using SmartScheduler.Application.Contracts.DTOs;
using SmartScheduler.Application.Contracts.Queries;

namespace SmartScheduler.Api.Endpoints.Dashboard;

/// <summary>
/// Dashboard API endpoints.
/// </summary>
public static class StatsEndpoint
{
    public static void MapDashboardEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/dashboard")
            .RequireAuthorization("Dispatcher")
            .WithTags("Dashboard")
            .WithOpenApi();

        // GET /api/dashboard/stats - Get dashboard statistics
        group.MapGet("/stats", async (
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var query = new GetDashboardStatisticsQuery();
            var statistics = await mediator.Send(query, cancellationToken);
            return Results.Ok(statistics);
        })
        .WithName("GetDashboardStatistics")
        .WithSummary("Get dashboard statistics")
        .WithDescription("Retrieves dashboard statistics including active contractors, pending jobs, average assignment time, and utilization rate. Results are cached for 5 minutes.")
        .Produces<DashboardStatisticsDto>(StatusCodes.Status200OK);
    }
}


