using MediatR;
using Microsoft.AspNetCore.Mvc;
using SmartScheduler.Application.Contracts.Commands;
using SmartScheduler.Application.Contracts.DTOs;
using SmartScheduler.Application.Contracts.Queries;

namespace SmartScheduler.Api.Endpoints.Admin;

/// <summary>
/// Admin weights configuration API endpoints.
/// </summary>
public static class WeightsEndpoints
{
    public static void MapWeightsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/admin/weights")
            .RequireAuthorization("Admin")
            .WithTags("Admin")
            .WithOpenApi();

        // GET /api/admin/weights/current - Get current active weights configuration
        group.MapGet("/current", async (
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var query = new GetCurrentWeightsConfigQuery();
            var result = await mediator.Send(query, cancellationToken);
            return Results.Ok(result);
        })
        .WithName("GetCurrentWeightsConfig")
        .WithSummary("Get current weights configuration")
        .WithDescription("Get the currently active scoring weights configuration")
        .Produces<WeightsConfigResponseDto>(StatusCodes.Status200OK);

        // GET /api/admin/weights/history - Get weights configuration history
        group.MapGet("/history", async (
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var query = new GetWeightsConfigHistoryQuery();
            var result = await mediator.Send(query, cancellationToken);
            return Results.Ok(result);
        })
        .WithName("GetWeightsConfigHistory")
        .WithSummary("Get weights configuration history")
        .WithDescription("Get history of all weights configuration versions")
        .Produces<IReadOnlyList<WeightsConfigHistoryItemDto>>(StatusCodes.Status200OK);

        // POST /api/admin/weights - Update weights configuration (creates new version)
        group.MapPost("/", async (
            IMediator mediator,
            [FromBody] UpdateWeightsConfigRequestDto request,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var userId = httpContext.User.FindFirst("sub")?.Value ?? httpContext.User.Identity?.Name ?? "Unknown";
                var command = new UpdateWeightsConfigCommand
                {
                    Request = request,
                    CreatedBy = userId
                };
                var result = await mediator.Send(command, cancellationToken);
                return Results.Created($"/api/admin/weights/current", result);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        })
        .WithName("UpdateWeightsConfig")
        .WithSummary("Update weights configuration")
        .WithDescription("Create a new version of the weights configuration")
        .Produces<WeightsConfigResponseDto>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest);

        // POST /api/admin/weights/rollback - Rollback to previous version
        group.MapPost("/rollback", async (
            IMediator mediator,
            [FromBody] RollbackWeightsConfigRequestDto request,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var userId = httpContext.User.FindFirst("sub")?.Value ?? httpContext.User.Identity?.Name ?? "Unknown";
                var command = new RollbackWeightsConfigCommand
                {
                    Request = request,
                    CreatedBy = userId
                };
                var result = await mediator.Send(command, cancellationToken);
                return Results.Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        })
        .WithName("RollbackWeightsConfig")
        .WithSummary("Rollback weights configuration")
        .WithDescription("Rollback to a previous version of the weights configuration")
        .Produces<WeightsConfigResponseDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);
    }
}

