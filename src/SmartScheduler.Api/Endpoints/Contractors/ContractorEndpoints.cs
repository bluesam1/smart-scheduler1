using MediatR;
using Microsoft.AspNetCore.Mvc;
using SmartScheduler.Application.Contracts.Commands;
using SmartScheduler.Application.Contracts.DTOs;
using SmartScheduler.Application.Contracts.Queries;

namespace SmartScheduler.Api.Endpoints.Contractors;

/// <summary>
/// Contractor API endpoints.
/// </summary>
public static class ContractorEndpoints
{
    public static void MapContractorEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/contractors")
            .RequireAuthorization("Dispatcher")
            .WithTags("Contractors")
            .WithOpenApi();

        // GET /api/contractors - List all contractors with optional filtering
        group.MapGet("/", async (
            IMediator mediator,
            [FromQuery] string? skills,
            [FromQuery] int? limit,
            CancellationToken cancellationToken) =>
        {
            var skillsList = !string.IsNullOrWhiteSpace(skills)
                ? skills.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                : null;

            var query = new GetContractorsQuery
            {
                Skills = skillsList,
                Limit = limit
            };

            var contractors = await mediator.Send(query, cancellationToken);
            return Results.Ok(contractors);
        })
        .WithName("GetContractors")
        .WithSummary("Get all contractors")
        .WithDescription("Retrieves a list of all contractors. Optionally filter by skills and limit results.")
        .Produces<IReadOnlyList<ContractorDto>>(StatusCodes.Status200OK);

        // GET /api/contractors/{id} - Get contractor by ID
        group.MapGet("/{id:guid}", async (
            IMediator mediator,
            Guid id,
            CancellationToken cancellationToken) =>
        {
            var query = new GetContractorByIdQuery { Id = id };
            var contractor = await mediator.Send(query, cancellationToken);

            if (contractor == null)
            {
                return Results.NotFound(new { message = $"Contractor with ID {id} not found." });
            }

            return Results.Ok(contractor);
        })
        .WithName("GetContractorById")
        .WithSummary("Get contractor by ID")
        .WithDescription("Retrieves a specific contractor by their unique identifier.")
        .Produces<ContractorDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // POST /api/contractors - Create new contractor
        group.MapPost("/", async (
            IMediator mediator,
            [FromBody] CreateContractorRequest request,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var command = new CreateContractorCommand { Request = request };
                var contractor = await mediator.Send(command, cancellationToken);
                return Results.Created($"/api/contractors/{contractor.Id}", contractor);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        })
        .WithName("CreateContractor")
        .WithSummary("Create a new contractor")
        .WithDescription("Creates a new contractor with the provided information.")
        .Produces<ContractorDto>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest);

        // PUT /api/contractors/{id} - Update existing contractor
        group.MapPut("/{id:guid}", async (
            IMediator mediator,
            Guid id,
            [FromBody] UpdateContractorRequest request,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var command = new UpdateContractorCommand
                {
                    Id = id,
                    Request = request
                };
                var contractor = await mediator.Send(command, cancellationToken);
                return Results.Ok(contractor);
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
        .WithName("UpdateContractor")
        .WithSummary("Update an existing contractor")
        .WithDescription("Updates an existing contractor with the provided information.")
        .Produces<ContractorDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);

        // DELETE /api/contractors/{id} - Delete contractor
        group.MapDelete("/{id:guid}", async (
            IMediator mediator,
            Guid id,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var command = new DeleteContractorCommand { Id = id };
                await mediator.Send(command, cancellationToken);
                return Results.NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
        })
        .WithName("DeleteContractor")
        .WithSummary("Delete a contractor")
        .WithDescription("Deletes a contractor by their unique identifier.")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound);

        // PUT /api/contractors/{id}/working-hours - Update working hours
        group.MapPut("/{id:guid}/working-hours", async (
            IMediator mediator,
            Guid id,
            [FromBody] IReadOnlyList<WorkingHoursDto> workingHours,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var command = new UpdateContractorWorkingHoursCommand
                {
                    ContractorId = id,
                    WorkingHours = workingHours
                };
                var contractor = await mediator.Send(command, cancellationToken);
                return Results.Ok(contractor);
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
        .WithName("UpdateContractorWorkingHours")
        .WithSummary("Update contractor working hours")
        .WithDescription("Updates the working hours for a contractor.")
        .Produces<ContractorDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);

        // POST /api/contractors/{id}/calendar/exceptions - Add calendar exception
        group.MapPost("/{id:guid}/calendar/exceptions", async (
            IMediator mediator,
            Guid id,
            [FromBody] CalendarExceptionDto exception,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var command = new AddCalendarExceptionCommand
                {
                    ContractorId = id,
                    Exception = exception
                };
                var contractor = await mediator.Send(command, cancellationToken);
                return Results.Ok(contractor);
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
        .WithName("AddCalendarException")
        .WithSummary("Add calendar exception")
        .WithDescription("Adds a calendar exception (holiday or override) to a contractor's calendar.")
        .Produces<ContractorDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);

        // DELETE /api/contractors/{id}/calendar/exceptions/{date} - Remove calendar exception
        group.MapDelete("/{id:guid}/calendar/exceptions/{date}", async (
            IMediator mediator,
            Guid id,
            string date,
            CancellationToken cancellationToken) =>
        {
            try
            {
                if (!DateOnly.TryParse(date, out var parsedDate))
                {
                    return Results.BadRequest(new { message = "Invalid date format. Use YYYY-MM-DD." });
                }

                var command = new RemoveCalendarExceptionCommand
                {
                    ContractorId = id,
                    Date = parsedDate
                };
                var contractor = await mediator.Send(command, cancellationToken);
                return Results.Ok(contractor);
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
        })
        .WithName("RemoveCalendarException")
        .WithSummary("Remove calendar exception")
        .WithDescription("Removes a calendar exception from a contractor's calendar by date (format: YYYY-MM-DD).")
        .Produces<ContractorDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);

        // PUT /api/contractors/{id}/rating - Update contractor rating
        group.MapPut("/{id:guid}/rating", async (
            IMediator mediator,
            Guid id,
            [FromBody] UpdateContractorRatingRequest request,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var command = new UpdateContractorRatingCommand
                {
                    ContractorId = id,
                    Rating = request.Rating
                };
                var contractor = await mediator.Send(command, cancellationToken);
                return Results.Ok(contractor);
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
            catch (ArgumentOutOfRangeException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        })
        .RequireAuthorization("Admin")
        .WithName("UpdateContractorRating")
        .WithSummary("Update contractor rating")
        .WithDescription("Updates a contractor's rating (Admin only). Rating must be between 0-100.")
        .Produces<ContractorDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);
    }
}

/// <summary>
/// Request DTO for updating contractor rating.
/// </summary>
public record UpdateContractorRatingRequest
{
    public int Rating { get; init; }
}

