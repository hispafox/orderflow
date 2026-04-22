using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Orders.API.Tests.Shared;

public static class JwtTestHelper
{
    private const string DefaultSigningKey = "orderflow-dev-signing-key-min-32-chars!!";
    private const string DefaultIssuer     = "orderflow-gateway";
    private const string DefaultAudience   = "orderflow";

    public static string GenerateToken(
        string?   userId        = null,
        string[]? roles         = null,
        string    signingKey    = DefaultSigningKey,
        string    issuer        = DefaultIssuer,
        string    audience      = DefaultAudience,
        int       expiryMinutes = 60)
    {
        var actualUserId = userId ?? Guid.NewGuid().ToString();
        var actualRoles  = roles ?? ["customer"];

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub,   actualUserId),
            new(JwtRegisteredClaimNames.Email, "test@techshop.es"),
            new(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
            new("name",                         "Test User"),
        };

        foreach (var role in actualRoles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
        var token = new JwtSecurityToken(
            issuer:             issuer,
            audience:           audience,
            claims:             claims,
            notBefore:          DateTime.UtcNow,
            expires:            DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static string CustomerToken(string? userId = null)
        => GenerateToken(userId, ["customer"]);

    public static string AdminToken(string? userId = null)
        => GenerateToken(userId, ["admin"]);

    public static string ExpiredToken()
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub,   Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Email, "test@techshop.es"),
            new(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
            new("name",                         "Test User"),
            new(ClaimTypes.Role, "customer"),
        };

        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(DefaultSigningKey));
        var token = new JwtSecurityToken(
            issuer:             DefaultIssuer,
            audience:           DefaultAudience,
            claims:             claims,
            notBefore:          DateTime.UtcNow.AddHours(-2),
            expires:            DateTime.UtcNow.AddHours(-1),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
