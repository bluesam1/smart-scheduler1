using MediatR;
using Microsoft.AspNetCore.Mvc;
using SmartScheduler.Application.Contracts.Commands;
using SmartScheduler.Application.Contracts.DTOs;
using SmartScheduler.Application.Contracts.Queries;

namespace SmartScheduler.Api.Endpoints.Settings;

/// <summary>
/// Settings API endpoints.
/// </summary>
public static class SettingsEndpoints
{
    public static void MapSettingsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/settings")
            .RequireAuthorization("Dispatcher")
            .WithTags("Settings")
            .WithOpenApi();

        // GET /api/settings/job-types - List all job types
        group.MapGet("/job-types", async (
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var query = new GetJobTypesQuery();
            var result = await mediator.Send(query, cancellationToken);
            return Results.Ok(result);
        })
        .WithName("GetJobTypes")
        .WithSummary("List available job types")
        .WithDescription("Get list of job types available for job creation")
        .Produces<JobTypesResponseDto>(StatusCodes.Status200OK);

        // POST /api/settings/job-types - Add new job type
        group.MapPost("/job-types", async (
            IMediator mediator,
            [FromBody] AddJobTypeRequestDto request,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var userId = httpContext.User.FindFirst("sub")?.Value ?? httpContext.User.Identity?.Name ?? "Unknown";
                var command = new AddJobTypeCommand
                {
                    Request = request,
                    UpdatedBy = userId
                };
                var result = await mediator.Send(command, cancellationToken);
                return Results.Created($"/api/settings/job-types", result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        })
        .WithName("AddJobType")
        .WithSummary("Add new job type")
        .WithDescription("Add a new job type to the system")
        .Produces<AddJobTypeResponseDto>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status409Conflict);

        // PUT /api/settings/job-types - Update job type
        group.MapPut("/job-types", async (
            IMediator mediator,
            [FromBody] UpdateJobTypeRequestDto request,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var userId = httpContext.User.FindFirst("sub")?.Value ?? httpContext.User.Identity?.Name ?? "Unknown";
                var command = new UpdateJobTypeCommand
                {
                    OldValue = request.OldValue,
                    NewValue = request.NewValue,
                    UpdatedBy = userId
                };
                await mediator.Send(command, cancellationToken);
                return Results.Ok();
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        })
        .WithName("UpdateJobType")
        .WithSummary("Update job type")
        .WithDescription("Rename an existing job type")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);

        // DELETE /api/settings/job-types?jobType=... - Remove job type
        group.MapDelete("/job-types", async (
            IMediator mediator,
            [FromQuery] string jobType,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var userId = httpContext.User.FindFirst("sub")?.Value ?? httpContext.User.Identity?.Name ?? "Unknown";
                var command = new DeleteJobTypeCommand 
                { 
                    JobType = jobType,
                    UpdatedBy = userId
                };
                await mediator.Send(command, cancellationToken);
                return Results.NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        })
        .WithName("DeleteJobType")
        .WithSummary("Remove job type")
        .WithDescription("Remove a job type from the system")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status409Conflict);

        // GET /api/settings/skills - List all skills
        group.MapGet("/skills", async (
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var query = new GetSkillsQuery();
            var result = await mediator.Send(query, cancellationToken);
            return Results.Ok(result);
        })
        .WithName("GetSkills")
        .WithSummary("List available skills")
        .WithDescription("Get list of skills available for contractors and job requirements")
        .Produces<SkillsResponseDto>(StatusCodes.Status200OK);

        // POST /api/settings/skills - Add new skill
        group.MapPost("/skills", async (
            IMediator mediator,
            [FromBody] AddSkillRequestDto request,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var userId = httpContext.User.FindFirst("sub")?.Value ?? httpContext.User.Identity?.Name ?? "Unknown";
                var command = new AddSkillCommand
                {
                    Skill = request.Skill,
                    UpdatedBy = userId
                };
                await mediator.Send(command, cancellationToken);
                return Results.Created($"/api/settings/skills", new { skill = request.Skill });
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        })
        .WithName("AddSkill")
        .WithSummary("Add new skill")
        .WithDescription("Add a new skill to the system")
        .Produces(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status409Conflict);

        // PUT /api/settings/skills - Update skill
        group.MapPut("/skills", async (
            IMediator mediator,
            [FromBody] UpdateSkillRequestDto request,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var userId = httpContext.User.FindFirst("sub")?.Value ?? httpContext.User.Identity?.Name ?? "Unknown";
                var command = new UpdateSkillCommand
                {
                    OldValue = request.OldValue,
                    NewValue = request.NewValue,
                    UpdatedBy = userId
                };
                await mediator.Send(command, cancellationToken);
                return Results.Ok();
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        })
        .WithName("UpdateSkill")
        .WithSummary("Update skill")
        .WithDescription("Rename an existing skill")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);

        // DELETE /api/settings/skills?skill=... - Remove skill
        group.MapDelete("/skills", async (
            IMediator mediator,
            [FromQuery] string skill,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var userId = httpContext.User.FindFirst("sub")?.Value ?? httpContext.User.Identity?.Name ?? "Unknown";
                var command = new DeleteSkillCommand 
                { 
                    Skill = skill,
                    UpdatedBy = userId
                };
                await mediator.Send(command, cancellationToken);
                return Results.NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        })
        .WithName("DeleteSkill")
        .WithSummary("Remove skill")
        .WithDescription("Remove a skill from the system")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status409Conflict);
    }
}

