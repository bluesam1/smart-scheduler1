using SmartScheduler.Infrastructure.Demo;

namespace SmartScheduler.Api.Endpoints.Admin;

/// <summary>
/// Admin demo data generation API endpoints.
/// </summary>
public static class DemoDataEndpoints
{
    public static void MapDemoDataEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/admin/demo-data")
            .RequireAuthorization("Admin")
            .WithTags("Admin")
            .WithOpenApi();

        // POST /api/admin/demo-data - Generate demo data
        group.MapPost("/", async (
            DemoDataService demoDataService,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await demoDataService.GenerateDemoDataAsync(cancellationToken);
                return Results.Ok(new
                {
                    contractorsCreated = result.ContractorsCreated,
                    jobsCreated = result.JobsCreated,
                    assignmentsCreated = result.AssignmentsCreated,
                    auditRecordsCreated = result.AuditRecordsCreated,
                    durationMs = result.Duration.TotalMilliseconds
                });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Error generating demo data",
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        })
        .WithName("GenerateDemoData")
        .WithSummary("Generate demo data")
        .WithDescription("Populates the database with realistic demo data across multiple US timezones")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status500InternalServerError);

        // DELETE /api/admin/demo-data - Delete all data
        group.MapDelete("/", async (
            DemoDataCleanupService cleanupService,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await cleanupService.DeleteAllDataAsync(cancellationToken);
                return Results.Ok(new
                {
                    contractorsDeleted = result.ContractorsDeleted,
                    jobsDeleted = result.JobsDeleted,
                    assignmentsDeleted = result.AssignmentsDeleted,
                    auditRecordsDeleted = result.AuditRecordsDeleted,
                    eventLogsDeleted = result.EventLogsDeleted,
                    durationMs = result.Duration.TotalMilliseconds
                });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Error deleting data",
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        })
        .WithName("DeleteAllData")
        .WithSummary("Delete all data")
        .WithDescription("WARNING: Deletes ALL data from the database (contractors, jobs, assignments, audit records, event logs). This operation cannot be undone!")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status500InternalServerError);

        // GET /api/admin/demo-data/counts - Get data counts
        group.MapGet("/counts", async (
            DemoDataCleanupService cleanupService,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var counts = await cleanupService.GetDataCountsAsync(cancellationToken);
                return Results.Ok(new
                {
                    contractors = counts.contractors,
                    jobs = counts.jobs,
                    assignments = counts.assignments,
                    auditRecords = counts.auditRecords,
                    eventLogs = counts.eventLogs
                });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Error getting data counts",
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        })
        .WithName("GetDataCounts")
        .WithSummary("Get data counts")
        .WithDescription("Gets counts of all data in the database")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status500InternalServerError);
    }
}

