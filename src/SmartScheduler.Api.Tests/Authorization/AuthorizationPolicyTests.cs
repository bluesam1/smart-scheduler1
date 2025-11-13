using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartScheduler.Api.Tests.Utilities;
using Xunit;

namespace SmartScheduler.Api.Tests.Authorization;

public class AuthorizationPolicyTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AuthorizationPolicyTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClientWithRole(string role)
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                // Remove all existing authentication services
                var authDescriptors = services.Where(s => 
                    s.ServiceType == typeof(Microsoft.AspNetCore.Authentication.IAuthenticationService) ||
                    s.ServiceType == typeof(Microsoft.AspNetCore.Authentication.IAuthenticationHandlerProvider) ||
                    s.ServiceType == typeof(Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider) ||
                    s.ServiceType.Name.Contains("Authentication")).ToList();
                
                foreach (var descriptor in authDescriptors)
                {
                    services.Remove(descriptor);
                }
                
                // Add test authentication as default scheme
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "Test";
                    options.DefaultChallengeScheme = "Test";
                })
                .AddScheme<TestAuthOptions, TestAuthHandler>(
                    "Test", options => options.Role = role);
            });
        }).CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task AdminEndpoint_RequiresAdminRole()
    {
        // Arrange
        var client = CreateClientWithRole("Admin");

        // Act
        var response = await client.GetAsync("/api/admin/test");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AdminEndpoint_BlocksNonAdminRole()
    {
        // Arrange
        var client = CreateClientWithRole("Dispatcher");

        // Act
        var response = await client.GetAsync("/api/admin/test");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DispatcherEndpoint_RequiresDispatcherOrAdminRole()
    {
        // Arrange
        var client = CreateClientWithRole("Dispatcher");

        // Act
        var response = await client.GetAsync("/api/dispatcher/test");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DispatcherEndpoint_AllowsAdminRole()
    {
        // Arrange
        var client = CreateClientWithRole("Admin");

        // Act
        var response = await client.GetAsync("/api/dispatcher/test");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DispatcherEndpoint_BlocksContractorRole()
    {
        // Arrange
        var client = CreateClientWithRole("Contractor");

        // Act
        var response = await client.GetAsync("/api/dispatcher/test");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ContractorEndpoint_RequiresContractorOrAdminRole()
    {
        // Arrange
        var client = CreateClientWithRole("Contractor");

        // Act
        var response = await client.GetAsync("/api/contractor/test");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ContractorEndpoint_AllowsAdminRole()
    {
        // Arrange
        var client = CreateClientWithRole("Admin");

        // Act
        var response = await client.GetAsync("/api/contractor/test");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ContractorEndpoint_BlocksDispatcherRole()
    {
        // Arrange
        var client = CreateClientWithRole("Dispatcher");

        // Act
        var response = await client.GetAsync("/api/contractor/test");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminEndpoint_BlocksUnauthorizedAccess()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/admin/test");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task HealthCheck_AllowsUnauthorizedAccess()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AuthenticatedEndpoint_RequiresAuthentication()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/authenticated/test");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

// Test authentication options
public class TestAuthOptions : AuthenticationSchemeOptions
{
    public string? Role { get; set; }
}

// Test authentication handler for integration tests
public class TestAuthHandler : AuthenticationHandler<TestAuthOptions>
{
    public TestAuthHandler(IOptionsMonitor<TestAuthOptions> options,
        ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var role = Options.Role ?? "Test";
        
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user"),
            new Claim(ClaimTypes.Email, "test@example.com"),
            new Claim(ClaimTypes.Role, role)
        };

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}


