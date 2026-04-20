using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Products.API.Infrastructure.Persistence;

namespace Products.API.Tests.Shared.Fixtures;

public class ProductsApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _testDbName = $"ProductsTestDb_{Guid.NewGuid():N}";

    private string ConnectionString =>
        $"Server=(localdb)\\MSSQLLocalDB;" +
        $"Database={_testDbName};" +
        $"Trusted_Connection=true;" +
        $"TrustServerCertificate=true;";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:sqlserver"] = ConnectionString,
                ["ConnectionStrings:messaging"] = "amqp://guest:guest@localhost:5672"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Replace MassTransit transport with in-memory test bus (no RabbitMQ needed)
            services.RemoveAll<IBusControl>();
            services.AddMassTransitTestHarness();
        });
    }

    public async Task InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    public new async Task DisposeAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
        await db.Database.EnsureDeletedAsync();
        await base.DisposeAsync();
    }
}
