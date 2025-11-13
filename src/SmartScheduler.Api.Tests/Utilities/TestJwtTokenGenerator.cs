using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace SmartScheduler.Api.Tests.Utilities;

/// <summary>
/// Utility class for generating test JWT tokens for integration tests.
/// Note: This generates tokens with a test signing key. For actual Cognito integration,
/// tokens must be validated against the Cognito User Pool.
/// </summary>
public static class TestJwtTokenGenerator
{
    private static readonly string TestIssuer = "https://cognito-idp.us-east-1.amazonaws.com/test-pool-id";
    private static readonly string TestAudience = "test-client-id";
    private static readonly SymmetricSecurityKey TestSigningKey = new(Encoding.UTF8.GetBytes("test-signing-key-that-is-at-least-32-characters-long-for-hmac-sha256"));

    /// <summary>
    /// Generates a test JWT token with the specified claims.
    /// </summary>
    public static string GenerateToken(string userId, string email, List<string> roles)
    {
        var claims = new List<Claim>
        {
            new Claim("sub", userId),
            new Claim("email", email),
            new Claim("cognito:username", userId),
        };

        // Add role claims from cognito:groups
        if (roles.Any())
        {
            claims.Add(new Claim("cognito:groups", string.Join(",", roles)));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = TestIssuer,
            Audience = TestAudience,
            SigningCredentials = new SigningCredentials(TestSigningKey, SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// Generates a test JWT token for an Admin user.
    /// </summary>
    public static string GenerateAdminToken(string userId = "admin-user", string email = "admin@example.com")
    {
        return GenerateToken(userId, email, new List<string> { "Admin" });
    }

    /// <summary>
    /// Generates a test JWT token for a Dispatcher user.
    /// </summary>
    public static string GenerateDispatcherToken(string userId = "dispatcher-user", string email = "dispatcher@example.com")
    {
        return GenerateToken(userId, email, new List<string> { "Dispatcher" });
    }

    /// <summary>
    /// Generates a test JWT token for a Contractor user.
    /// </summary>
    public static string GenerateContractorToken(string userId = "contractor-user", string email = "contractor@example.com")
    {
        return GenerateToken(userId, email, new List<string> { "Contractor" });
    }
}

