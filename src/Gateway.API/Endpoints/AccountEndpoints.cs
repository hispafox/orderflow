using System.ComponentModel.DataAnnotations;
using Gateway.API.Data;
using Gateway.API.Services;
using Microsoft.AspNetCore.Identity;

namespace Gateway.API.Endpoints;

public static class AccountEndpoints
{
    public static WebApplication MapAccountEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/account")
            .WithTags("Auth")
            .AllowAnonymous();

        group.MapPost("/login", async (
            LoginRequest                   request,
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser>   userManager,
            JwtService                     jwtService) =>
        {
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user is null)
                return Results.Problem(
                    title:      "Invalid credentials",
                    detail:     "Email or password is incorrect",
                    statusCode: StatusCodes.Status401Unauthorized);

            var result = await signInManager.CheckPasswordSignInAsync(
                user, request.Password, lockoutOnFailure: true);

            if (!result.Succeeded)
            {
                var detail = result.IsLockedOut
                    ? "Account is locked out"
                    : "Email or password is incorrect";
                return Results.Problem(
                    title:      "Invalid credentials",
                    detail:     detail,
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var token = await jwtService.GenerateTokenAsync(user);

            return Results.Ok(new
            {
                access_token = token,
                token_type   = "Bearer",
                expires_in   = 3600,
                user_id      = user.Id,
                email        = user.Email,
                full_name    = user.FullName
            });
        });

        return app;
    }
}

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required]               string Password);
