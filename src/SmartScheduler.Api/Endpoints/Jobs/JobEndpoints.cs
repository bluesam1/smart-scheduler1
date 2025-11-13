using MediatR;
using Microsoft.AspNetCore.Mvc;
using SmartScheduler.Application.Contracts.Commands;
using SmartScheduler.Application.Contracts.DTOs;
using SmartScheduler.Application.Contracts.Queries;

namespace SmartScheduler.Api.Endpoints.Jobs;

/// <summary>
/// Job API endpoints.
/// </summary>
public static class JobEndpoints
{
    public static void MapJobEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/jobs")
            .RequireAuthorization("Dispatcher")
            .WithTags("Jobs")
            .WithOpenApi();

        // GET /api/jobs - List all jobs with optional filtering
        group.MapGet("/", async (
            IMediator mediator,
            [FromQuery] string? status,
            [FromQuery] string? priority,
            [FromQuery] int? limit,
            CancellationToken cancellationToken) =>
        {
            var query = new GetJobsQuery
            {
                Status = status,
                Priority = priority,
                Limit = limit
            };

            var jobs = await mediator.Send(query, cancellationToken);
            return Results.Ok(jobs);
        })
        .WithName("GetJobs")
        .WithSummary("Get all jobs")
        .WithDescription("Retrieves a list of all jobs. Optionally filter by status and priority, and limit results.")
        .Produces<IReadOnlyList<JobDto>>(StatusCodes.Status200OK);

        // GET /api/jobs/{id} - Get job by ID
        group.MapGet("/{id:guid}", async (
            IMediator mediator,
            Guid id,
            CancellationToken cancellationToken) =>
        {
            var query = new GetJobByIdQuery { Id = id };
            var job = await mediator.Send(query, cancellationToken);

            if (job == null)
            {
                return Results.NotFound(new { message = $"Job with ID {id} not found." });
            }

            return Results.Ok(job);
        })
        .WithName("GetJobById")
        .WithSummary("Get job by ID")
        .WithDescription("Retrieves a specific job by its unique identifier.")
        .Produces<JobDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // POST /api/jobs - Create new job
        group.MapPost("/", async (
            IMediator mediator,
            [FromBody] CreateJobRequest request,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var command = new CreateJobCommand { Request = request };
                var job = await mediator.Send(command, cancellationToken);
                return Results.Created($"/api/jobs/{job.Id}", job);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
            catch (ArgumentNullException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        })
        .WithName("CreateJob")
        .WithSummary("Create a new job")
        .WithDescription("Creates a new job with the provided information. Address validation and timezone lookup are performed automatically.")
        .Produces<JobDto>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest);

        // PUT /api/jobs/{id} - Update existing job
        group.MapPut("/{id:guid}", async (
            IMediator mediator,
            Guid id,
            [FromBody] UpdateJobRequest request,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var command = new UpdateJobCommand
                {
                    Id = id,
                    Request = request
                };
                var job = await mediator.Send(command, cancellationToken);
                return Results.Ok(job);
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
            catch (ArgumentOutOfRangeException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
            catch (ArgumentNullException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        })
        .WithName("UpdateJob")
        .WithSummary("Update an existing job")
        .WithDescription("Updates an existing job with the provided information. Only provided fields will be updated.")
        .Produces<JobDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);

        // PUT /api/jobs/{id}/status - Update job status
        group.MapPut("/{id:guid}/status", async (
            IMediator mediator,
            Guid id,
            [FromBody] UpdateJobStatusRequest request,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var command = new UpdateJobStatusCommand
                {
                    JobId = id,
                    NewStatus = request.Status
                };
                var job = await mediator.Send(command, cancellationToken);
                return Results.Ok(job);
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        })
        .WithName("UpdateJobStatus")
        .WithSummary("Update job status")
        .WithDescription("Updates the status of an existing job. Valid status transitions are enforced.")
        .Produces<JobDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);

        // POST /api/jobs/{id}/assign - Assign job to contractor
        group.MapPost("/{id:guid}/assign", async (
            IMediator mediator,
            Guid id,
            [FromBody] AssignJobRequest request,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var command = new AssignJobCommand
                {
                    JobId = id,
                    Request = request
                };
                var assignment = await mediator.Send(command, cancellationToken);
                return Results.Created($"/api/jobs/{id}/assignments/{assignment.Id}", assignment);
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                // Conflict or availability issue
                return Results.Conflict(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        })
        .WithName("AssignJob")
        .WithSummary("Assign job to contractor")
        .WithDescription("Assigns a job to a contractor with the specified time slot. Availability is re-validated before assignment. Returns existing assignment if same request (idempotent).")
        .Produces<AssignmentDto>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status409Conflict);

        // PUT /api/jobs/{id}/reschedule - Reschedule job
        group.MapPut("/{id:guid}/reschedule", async (
            IMediator mediator,
            Guid id,
            [FromBody] RescheduleJobRequest request,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var command = new RescheduleJobCommand
                {
                    JobId = id,
                    Request = request
                };
                var job = await mediator.Send(command, cancellationToken);
                return Results.Ok(job);
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                // Conflict or availability issue
                return Results.Conflict(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        })
        .WithName("RescheduleJob")
        .WithSummary("Reschedule job")
        .WithDescription("Reschedules a job to a new time slot. Validates feasibility for all assigned contractors and updates assignments accordingly.")
        .Produces<JobDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status409Conflict);

        // POST /api/jobs/{id}/cancel - Cancel job
        group.MapPost("/{id:guid}/cancel", async (
            IMediator mediator,
            Guid id,
            [FromBody] CancelJobRequest? request,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var command = new CancelJobCommand
                {
                    JobId = id,
                    Request = request
                };
                var job = await mediator.Send(command, cancellationToken);
                return Results.Ok(job);
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        })
        .WithName("CancelJob")
        .WithSummary("Cancel job")
        .WithDescription("Cancels a job and all its active assignments. Cannot cancel completed jobs.")
        .Produces<JobDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);
    }
}

/// <summary>
/// Request DTO for updating job status.
/// </summary>
public record UpdateJobStatusRequest
{
    public SmartScheduler.Domain.Contracts.ValueObjects.JobStatus Status { get; init; }
}

