using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Gateway.API.Data;

public class GatewayIdentityDbContext : IdentityDbContext<ApplicationUser>
{
    public GatewayIdentityDbContext(
        DbContextOptions<GatewayIdentityDbContext> options)
        : base(options) { }
}
