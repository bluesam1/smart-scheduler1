using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

namespace SmartScheduler.Api.Authentication;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddCognitoAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var cognitoRegion = configuration["Cognito:Region"] ?? "us-east-1";
        var userPoolId = configuration["Cognito:UserPoolId"] ?? throw new InvalidOperationException("Cognito:UserPoolId is required");
        var appClientId = configuration["Cognito:AppClientId"] ?? throw new InvalidOperationException("Cognito:AppClientId is required");
        
        var issuer = $"https://cognito-idp.{cognitoRegion}.amazonaws.com/{userPoolId}";
        
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            // Don't challenge anonymous requests - only challenge when authorization is explicitly required
            options.DefaultForbidScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.Authority = issuer;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = false, // Disable audience validation - Cognito access tokens may not include it
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = issuer,
                // If audience is present, validate against app client ID
                ValidAudience = appClientId,
                ClockSkew = TimeSpan.Zero
            };
            
            // Custom audience validator - only validate if audience claim is present
            options.TokenValidationParameters.AudienceValidator = (audiences, securityToken, validationParameters) =>
            {
                // If no audience claim, accept the token (Cognito access tokens may omit it)
                if (audiences == null || !audiences.Any())
                {
                    return true;
                }
                
                // If audience is present, validate it matches the app client ID
                return audiences.Contains(appClientId);
            };
            
            // Configure SignalR to accept JWT tokens from both headers and query string
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var path = context.HttpContext.Request.Path;
                    
                    // Only check query string for SignalR hub paths
                    // Support both /hub and /hubs paths for SignalR
                    if (path.StartsWithSegments("/hub") || path.StartsWithSegments("/hubs"))
                    {
                        // Check for token in query string (used by WebSocket connections)
                        var accessToken = context.Request.Query["access_token"];
                        
                        if (!string.IsNullOrEmpty(accessToken))
                        {
                            context.Token = accessToken;
                        }
                    }
                    
                    return Task.CompletedTask;
                },
                OnAuthenticationFailed = context =>
                {
                    var loggerFactory = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>();
                    var logger = loggerFactory.CreateLogger("Authentication");
                    logger.LogError("Authentication failed: {Error}", context.Exception?.Message);
                    logger.LogError("Exception details: {Exception}", context.Exception);
                    return Task.CompletedTask;
                },
                OnChallenge = context =>
                {
                    var loggerFactory = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>();
                    var logger = loggerFactory.CreateLogger("Authentication");
                    logger.LogWarning("Authentication challenge: {Error}, {ErrorDescription}", 
                        context.Error, context.ErrorDescription);
                    
                    // Log if token is present
                    var authHeader = context.HttpContext.Request.Headers["Authorization"].ToString();
                    if (!string.IsNullOrEmpty(authHeader))
                    {
                        logger.LogInformation("Authorization header present: {Header}", 
                            authHeader.Substring(0, Math.Min(50, authHeader.Length)) + "...");
                    }
                    else
                    {
                        logger.LogWarning("No Authorization header found in request");
                    }
                    
                    return Task.CompletedTask;
                },
                OnTokenValidated = context =>
                {
                    var loggerFactory = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>();
                    var logger = loggerFactory.CreateLogger("Authentication");
                    
                    // Log all claims for debugging
                    var allClaims = context.Principal?.Claims.Select(c => $"{c.Type}={c.Value}").ToList() ?? new List<string>();
                    logger.LogInformation("Token validated. Claims: {Claims}", string.Join(", ", allClaims));
                    
                    // Map Cognito groups claim to ASP.NET Core roles
                    var groups = context.Principal?.Claims
                        .Where(c => c.Type == "cognito:groups")
                        .SelectMany(c => c.Value.Split(','))
                        .ToList() ?? new List<string>();
                    
                    if (groups.Any())
                    {
                        logger.LogInformation("Found Cognito groups: {Groups}", string.Join(", ", groups));
                    }
                    else
                    {
                        logger.LogWarning("No cognito:groups claim found in token. User may not be assigned to any groups.");
                    }
                    
                    foreach (var group in groups)
                    {
                        var identity = context.Principal?.Identity as System.Security.Claims.ClaimsIdentity;
                        identity?.AddClaim(new System.Security.Claims.Claim(
                            System.Security.Claims.ClaimTypes.Role,
                            group.Trim()));
                    }
                    
                    // Log final roles
                    var roles = context.Principal?.Claims
                        .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
                        .Select(c => c.Value)
                        .ToList() ?? new List<string>();
                    logger.LogInformation("User roles after mapping: {Roles}", string.Join(", ", roles));
                    
                    return Task.CompletedTask;
                }
            };
        });
        
        return services;
    }
    
    public static IServiceCollection AddRoleBasedAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            // IMPORTANT: Don't set a FallbackPolicy - this would require auth for ALL endpoints
            // Only endpoints with .RequireAuthorization() will require authentication
            
            // Admin policy - full access
            options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
            
            // Dispatcher policy - can view jobs, request recommendations, confirm bookings
            options.AddPolicy("Dispatcher", policy => policy.RequireRole("Dispatcher", "Admin"));
            
            // Contractor policy - can view own assignments and schedule
            options.AddPolicy("Contractor", policy => policy.RequireRole("Contractor", "Admin"));
        });
        
        return services;
    }
}

