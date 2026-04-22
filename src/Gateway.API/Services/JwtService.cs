using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Gateway.API.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace Gateway.API.Services;

public class JwtService
{
    private readonly IConfiguration               _config;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<JwtService>          _logger;

    public JwtService(
        IConfiguration               config,
        UserManager<ApplicationUser> userManager,
        ILogger<JwtService>          logger)
    {
        _config      = config;
        _userManager = userManager;
        _logger      = logger;
    }

    public async Task<string> GenerateTokenAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub,   user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
            new("name",                         user.FullName ?? user.Email!),
        };

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var key          = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:SigningKey"]!));
        var expiryMinutes = int.Parse(_config["Jwt:ExpiryMinutes"] ?? "60");

        var token = new JwtSecurityToken(
            issuer:             _config["Jwt:Issuer"],
            audience:           _config["Jwt:Audience"],
            claims:             claims,
            notBefore:          DateTime.UtcNow,
            expires:            DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        _logger.LogInformation(
            "JWT generated for user {UserId} with roles [{Roles}], expires in {Minutes} min",
            user.Id, string.Join(", ", roles), expiryMinutes);

        return tokenString;
    }
}
