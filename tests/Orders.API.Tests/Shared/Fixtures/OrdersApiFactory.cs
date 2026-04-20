using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Orders.API.Infrastructure.Http;
using Orders.API.Infrastructure.Persistence;
using RabbitMQ.Client;

namespace Orders.API.Tests.Shared.Fixtures;

public class OrdersApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _testDbName = $"OrdersTestDb_{Guid.NewGuid():N}";

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
                ["ConnectionStrings:messaging"] = "amqp://guest:guest@localhost:5672",
                ["Jwt:SigningKey"]               = "orderflow-dev-signing-key-min-32-chars!!",
                ["Jwt:Issuer"]                   = "orderflow-gateway",
                ["Jwt:Audience"]                 = "orderflow"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Mock IConnection (RabbitMQ health check)
            services.RemoveAll<IConnection>();
            services.AddSingleton(Substitute.For<IConnection>());

            // Replace MassTransit transport with in-memory test bus
            services.AddMassTransitTestHarness();

            // Replace ProductsClient with a mock that returns a valid product
            services.RemoveAll<ProductsClient>();
            var mockClient = Substitute.ForPartsOf<ProductsClient>(
                new System.Net.Http.HttpClient { BaseAddress = new Uri("http://test") },
                new MemoryCache(new MemoryCacheOptions()),
                NullLogger<ProductsClient>.Instance);
            mockClient.GetProductAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                      .Returns(new ProductDetailDto(
                          Guid.NewGuid(), "Test Product", 99.99m, "EUR", 100, true, Guid.Empty));
            services.AddSingleton(mockClient);
        });
    }

    public async Task InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    public new async Task DisposeAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        await db.Database.EnsureDeletedAsync();
        await base.DisposeAsync();
    }
}
