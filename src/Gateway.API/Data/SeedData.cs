using Microsoft.AspNetCore.Identity;

namespace Gateway.API.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var logger      = services.GetRequiredService<ILogger<GatewayIdentityDbContext>>();

        foreach (var role in new[] { "admin", "customer" })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
                logger.LogInformation("Role '{Role}' created", role);
            }
        }

        if (await userManager.FindByEmailAsync("admin@techshop.es") is null)
        {
            var admin = new ApplicationUser
            {
                UserName = "admin@techshop.es",
                Email    = "admin@techshop.es",
                FullName = "Admin TechShop"
            };
            var result = await userManager.CreateAsync(admin, "Admin1234!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, "admin");
                logger.LogInformation("Admin user created: admin@techshop.es");
            }
            else
            {
                logger.LogError("Failed to create admin: {Errors}",
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }

        if (await userManager.FindByEmailAsync("john@techshop.es") is null)
        {
            var john = new ApplicationUser
            {
                UserName = "john@techshop.es",
                Email    = "john@techshop.es",
                FullName = "John Doe"
            };
            var result = await userManager.CreateAsync(john, "Customer1234!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(john, "customer");
                logger.LogInformation("Customer user created: john@techshop.es");
            }
        }
    }
}
