using Microsoft.EntityFrameworkCore;
using SmartScheduler.Infrastructure.Data;
using SmartScheduler.Api.Authentication;
using SmartScheduler.Realtime.Hubs;
using SmartScheduler.Realtime.Services;
using Serilog;
using MediatR;
using SmartScheduler.Application.Contracts;
using SmartScheduler.Domain.Contracts.Repositories;
using SmartScheduler.Infrastructure.Contracts.Repositories;
using SmartScheduler.Api.Endpoints.Contractors;
using SmartScheduler.Api.Endpoints.Jobs;
using SmartScheduler.Api.Endpoints.Recommendations;
using SmartScheduler.Api.Endpoints.Settings;
using SmartScheduler.Api.Endpoints.Admin;
using SmartScheduler.Api.Endpoints.Dashboard;
using SmartScheduler.Api.Endpoints.Activity;
using Polly;
using Polly.Extensions.Http;
using SmartScheduler.Application.Contracts.Services;
using SmartScheduler.Infrastructure.ExternalServices;
using SmartScheduler.Application.Recommendations.Configuration;
using SmartScheduler.Infrastructure.Configuration;
using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/smartscheduler-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddEndpointsApiExplorer();

// Configure NSwag for OpenAPI/Swagger
builder.Services.AddOpenApiDocument(config =>
{
    config.Title = "SmartScheduler API";
    config.Version = "v1";
    config.DocumentName = "v1";
    config.Description = "SmartScheduler API for job scheduling and contractor management";
});

// Configure Entity Framework Core with PostgreSQL
builder.Services.AddDbContext<SmartSchedulerDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions => npgsqlOptions.UseNetTopologySuite()));

// Add health checks
builder.Services.AddHealthChecks();

// Add authentication and authorization
builder.Services.AddCognitoAuthentication(builder.Configuration);
builder.Services.AddRoleBasedAuthorization();

// Add SignalR
builder.Services.AddSignalR();

// Register real-time publisher service
builder.Services.AddScoped<IRealtimePublisher, SignalRRealtimePublisher>();

// Register MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ApplicationContractsAssemblyMarker).Assembly));

// Register repositories
builder.Services.AddScoped<IContractorRepository, ContractorRepository>();
builder.Services.AddScoped<SmartScheduler.Domain.Contracts.Repositories.IJobRepository, SmartScheduler.Infrastructure.Contracts.Repositories.JobRepository>();
builder.Services.AddScoped<SmartScheduler.Domain.Contracts.Repositories.IAuditRecommendationRepository, SmartScheduler.Infrastructure.Contracts.Repositories.AuditRecommendationRepository>();
builder.Services.AddScoped<SmartScheduler.Domain.Contracts.Repositories.IAssignmentRepository, SmartScheduler.Infrastructure.Contracts.Repositories.AssignmentRepository>();
builder.Services.AddScoped<SmartScheduler.Domain.Contracts.Repositories.ISystemConfigurationRepository, SmartScheduler.Infrastructure.Contracts.Repositories.SystemConfigurationRepository>();
builder.Services.AddScoped<SmartScheduler.Domain.Contracts.Repositories.IWeightsConfigRepository, SmartScheduler.Infrastructure.Contracts.Repositories.WeightsConfigRepository>();
builder.Services.AddScoped<SmartScheduler.Domain.Contracts.Repositories.IEventLogRepository, SmartScheduler.Infrastructure.Contracts.Repositories.EventLogRepository>();

// Register services
builder.Services.AddScoped<SmartScheduler.Application.Contracts.Services.ISkillNormalizationService, SmartScheduler.Application.Contracts.Services.SkillNormalizationService>();
builder.Services.AddScoped<SmartScheduler.Application.Contracts.Services.IAvailabilityRevalidator, SmartScheduler.Application.Contracts.Services.AvailabilityRevalidator>();

// Register demo data services
builder.Services.AddScoped<SmartScheduler.Infrastructure.Demo.DemoDataService>();
builder.Services.AddScoped<SmartScheduler.Infrastructure.Demo.DemoDataCleanupService>();

// Register memory cache
builder.Services.AddMemoryCache();

// Register Google Places client
var googlePlacesApiKey = builder.Configuration["GooglePlaces:ApiKey"] ?? 
                         Environment.GetEnvironmentVariable("GOOGLE_PLACES_API_KEY") ?? 
                         string.Empty;
builder.Services.AddHttpClient<SmartScheduler.Infrastructure.ExternalServices.GooglePlacesClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddSingleton(provider =>
{
    var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient(nameof(SmartScheduler.Infrastructure.ExternalServices.GooglePlacesClient));
    var cache = provider.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
    var logger = provider.GetRequiredService<ILogger<SmartScheduler.Infrastructure.ExternalServices.GooglePlacesClient>>();
    return new SmartScheduler.Infrastructure.ExternalServices.GooglePlacesClient(httpClient, cache, logger, googlePlacesApiKey);
});

// Register timezone service
builder.Services.AddHttpClient<SmartScheduler.Infrastructure.Services.TimezoneService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddScoped(provider =>
{
    var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient(nameof(SmartScheduler.Infrastructure.Services.TimezoneService));
    var cache = provider.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
    var logger = provider.GetRequiredService<ILogger<SmartScheduler.Infrastructure.Services.TimezoneService>>();
    return new SmartScheduler.Infrastructure.Services.TimezoneService(httpClient, cache, logger);
});

// Register address validation service
builder.Services.AddScoped<SmartScheduler.Application.Contracts.Services.IAddressValidationService, SmartScheduler.Infrastructure.Services.AddressValidationService>();
builder.Services.AddScoped<SmartScheduler.Application.Contracts.Services.ITimezoneService>(provider =>
    provider.GetRequiredService<SmartScheduler.Infrastructure.Services.TimezoneService>());

// Register ETA matrix service
builder.Services.AddScoped<SmartScheduler.Application.Contracts.Services.IETAMatrixService, SmartScheduler.Application.Contracts.Services.ETAMatrixService>();

// Register cached distance service
builder.Services.AddScoped<SmartScheduler.Application.Contracts.Services.IDistanceService, SmartScheduler.Infrastructure.Services.CachedDistanceService>();

// Register distance calculation service with fallback
builder.Services.AddScoped<SmartScheduler.Application.Contracts.Services.IDistanceCalculationService, SmartScheduler.Application.Contracts.Services.DistanceCalculationService>();

// Register batch distance processor
builder.Services.AddScoped<SmartScheduler.Application.Contracts.Services.IBatchDistanceProcessor, SmartScheduler.Application.Contracts.Services.BatchDistanceProcessor>();

// Register scoring weights configuration loader
builder.Services.AddSingleton<IScoringWeightsConfigLoader, ScoringWeightsConfigLoader>();

// Register scoring service
builder.Services.AddScoped<SmartScheduler.Application.Recommendations.Services.IScoringService, SmartScheduler.Application.Recommendations.Services.ScoringService>();

// Register tie-breaker service
builder.Services.AddScoped<SmartScheduler.Application.Recommendations.Services.ITieBreakerService, SmartScheduler.Application.Recommendations.Services.TieBreakerService>();

// Register rotation boost service
builder.Services.AddScoped<SmartScheduler.Application.Recommendations.Services.IRotationBoostService, SmartScheduler.Application.Recommendations.Services.RotationBoostService>();

// Register rationale generator
builder.Services.AddScoped<SmartScheduler.Application.Recommendations.Services.IRationaleGenerator, SmartScheduler.Application.Recommendations.Services.RationaleGenerator>();

// Register domain scheduling services
builder.Services.AddScoped<SmartScheduler.Domain.Scheduling.Services.IAvailabilityEngine, SmartScheduler.Domain.Scheduling.Services.AvailabilityEngine>();
builder.Services.AddScoped<SmartScheduler.Domain.Scheduling.Services.ITravelBufferService, SmartScheduler.Domain.Scheduling.Services.TravelBufferService>();
builder.Services.AddScoped<SmartScheduler.Domain.Scheduling.Services.IFatigueCalculator, SmartScheduler.Domain.Scheduling.Services.FatigueCalculator>();
builder.Services.AddScoped<SmartScheduler.Domain.Scheduling.Services.ISlotGenerator, SmartScheduler.Domain.Scheduling.Services.SlotGenerator>();

// Register calendar consistency checker
builder.Services.AddScoped<SmartScheduler.Application.Scheduling.Services.ICalendarConsistencyChecker, SmartScheduler.Application.Scheduling.Services.CalendarConsistencyChecker>();

// Register OpenRouteService client with Polly resilience
var orsBaseUrl = builder.Configuration["OpenRouteService:BaseUrl"] ?? 
                 Environment.GetEnvironmentVariable("ORS_BASE_URL") ?? 
                 "https://api.openrouteservice.org";
var orsApiKey = builder.Configuration["OpenRouteService:ApiKey"] ?? 
                Environment.GetEnvironmentVariable("ORS_API_KEY") ?? 
                string.Empty;
var orsTimeoutMs = builder.Configuration.GetValue<int>("OpenRouteService:TimeoutMs", 3500);

// Configure Polly policies: retry with exponential backoff and circuit breaker
var retryPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
    .WaitAndRetryAsync(
        retryCount: 2,
        sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 1000)),
        onRetry: (outcome, timespan, retryCount, context) =>
        {
            // Log retry attempt
            var loggerFactory = builder.Services.BuildServiceProvider().GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<OpenRouteServiceClient>();
            logger.LogWarning("Retrying ORS API call. Attempt {RetryCount} after {Delay}ms", retryCount, timespan.TotalMilliseconds);
        });

var circuitBreakerPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .CircuitBreakerAsync(
        handledEventsAllowedBeforeBreaking: 5,
        durationOfBreak: TimeSpan.FromSeconds(30),
        onBreak: (exception, duration) =>
        {
            var loggerFactory = builder.Services.BuildServiceProvider().GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<OpenRouteServiceClient>();
            logger.LogWarning("Circuit breaker opened for ORS API. Will remain open for {Duration}s", duration.TotalSeconds);
        },
        onReset: () =>
        {
            var loggerFactory = builder.Services.BuildServiceProvider().GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<OpenRouteServiceClient>();
            logger.LogInformation("Circuit breaker reset for ORS API");
        });

var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromMilliseconds(orsTimeoutMs));

var resiliencePolicy = Policy.WrapAsync(retryPolicy, circuitBreakerPolicy, timeoutPolicy);

builder.Services.AddHttpClient<OpenRouteServiceClient>(client =>
{
    client.BaseAddress = new Uri(orsBaseUrl);
    client.Timeout = TimeSpan.FromMilliseconds(orsTimeoutMs);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
})
.AddPolicyHandler(resiliencePolicy)
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler());

builder.Services.AddSingleton<IOpenRouteServiceClient>(provider =>
{
    var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient(nameof(OpenRouteServiceClient));
    var logger = provider.GetRequiredService<ILogger<OpenRouteServiceClient>>();
    return new OpenRouteServiceClient(httpClient, logger, orsApiKey, orsBaseUrl);
});

// Configure CORS for frontend
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        // Get allowed origins from configuration (supports multiple origins)
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
            ?? new[] { "http://localhost:3000" };
        
        // Filter out wildcard patterns and add them separately
        var specificOrigins = allowedOrigins.Where(o => !o.Contains("*")).ToArray();
        var wildcardPatterns = allowedOrigins.Where(o => o.Contains("*")).ToArray();
        
        // If we have wildcard patterns, use SetIsOriginAllowed for dynamic checking
        // Otherwise, use WithOrigins for better performance
        if (wildcardPatterns.Length > 0)
        {
            policy.SetIsOriginAllowed(origin =>
            {
                if (string.IsNullOrEmpty(origin))
                    return false;
                
                // Check against specific origins first
                if (specificOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
                    return true;
                
                // Check against wildcard patterns
                foreach (var pattern in wildcardPatterns)
                {
                    var regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
                    if (Regex.IsMatch(origin, regexPattern, RegexOptions.IgnoreCase))
                        return true;
                }
                
                return false;
            });
        }
        else if (specificOrigins.Length > 0)
        {
            policy.WithOrigins(specificOrigins);
        }
        
        policy.AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Apply database migrations on startup
// In Development: automatically apply migrations
// In Production: controlled via configuration (default: false for safety)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var context = services.GetRequiredService<SmartSchedulerDbContext>();
    
    try
    {
        // Check if auto-migration is enabled (default: true in Development, false in Production)
        var autoMigrate = builder.Configuration.GetValue<bool>("Database:AutoMigrate", app.Environment.IsDevelopment());
        
        if (autoMigrate)
        {
            logger.LogInformation("Applying database migrations...");
            context.Database.Migrate();
            logger.LogInformation("Database migrations applied successfully.");
        }
        else
        {
            logger.LogInformation("Auto-migration is disabled. Migrations must be applied manually.");
            
            // Check if migrations are pending
            var pendingMigrations = context.Database.GetPendingMigrations().ToList();
            if (pendingMigrations.Any())
            {
                logger.LogWarning(
                    "There are {Count} pending migration(s): {Migrations}. " +
                    "Consider running migrations manually or enabling AutoMigrate.",
                    pendingMigrations.Count,
                    string.Join(", ", pendingMigrations));
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while applying database migrations.");
        
        // In Development, we might want to continue anyway
        // In Production, we should fail fast
        if (!app.Environment.IsDevelopment())
        {
            throw; // Fail fast in production
        }
        
        logger.LogWarning("Continuing despite migration error (Development mode).");
    }
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseOpenApi();
    app.UseSwaggerUi();
}

app.UseHttpsRedirection();
app.UseCors();

// Authentication and Authorization middleware (must be after CORS, before endpoints)
app.UseAuthentication();
app.UseAuthorization();

// Health check endpoint (public, no auth required)
app.MapHealthChecks("/health");

// SignalR hub endpoint (requires authentication)
app.MapHub<RecommendationsHub>("/hub/recommendations")
    .RequireAuthorization();

// Minimal API endpoint example (requires authentication)
app.MapGet("/", () => Results.Json(new { message = "SmartScheduler API", version = "1.0.0" }))
    .RequireAuthorization()
    .WithName("GetRoot")
    .WithOpenApi();

// Test endpoints for role-based authorization
app.MapGet("/api/admin/test", () => Results.Ok(new { message = "Admin access granted" }))
    .RequireAuthorization("Admin")
    .WithName("AdminTest")
    .WithOpenApi();

app.MapGet("/api/dispatcher/test", () => Results.Ok(new { message = "Dispatcher access granted" }))
    .RequireAuthorization("Dispatcher")
    .WithName("DispatcherTest")
    .WithOpenApi();

app.MapGet("/api/contractor/test", () => Results.Ok(new { message = "Contractor access granted" }))
    .RequireAuthorization("Contractor")
    .WithName("ContractorTest")
    .WithOpenApi();

app.MapGet("/api/authenticated/test", () => Results.Ok(new { message = "Authenticated access granted" }))
    .RequireAuthorization()
    .WithName("AuthenticatedTest")
    .WithOpenApi();

// Map contractor endpoints
app.MapContractorEndpoints();

// Map job endpoints
app.MapJobEndpoints();

// Map recommendation endpoints
app.MapRecommendationEndpoints();

// Map settings endpoints
app.MapSettingsEndpoints();

// Map admin weights endpoints
app.MapWeightsEndpoints();

// Map admin demo data endpoints
app.MapDemoDataEndpoints();

// Map dashboard endpoints
app.MapDashboardEndpoints();

// Map activity endpoints
app.MapActivityEndpoints();

app.Run();

// Make Program class accessible for testing
public partial class Program { }
