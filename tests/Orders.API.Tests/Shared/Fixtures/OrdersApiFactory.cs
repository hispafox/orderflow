using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using RabbitMQ.Client;

namespace Orders.API.Tests.Shared.Fixtures;

public class OrdersApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:sqlserver"] = "Server=(localdb)\\MSSQLLocalDB;Trusted_Connection=true;TrustServerCertificate=true;",
                ["ConnectionStrings:messaging"] = "amqp://guest:guest@localhost:5672"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Reemplazar IConnection real por substitute — evita conectar a RabbitMQ en tests
            services.RemoveAll<IConnection>();
            services.AddSingleton(Substitute.For<IConnection>());
        });
    }
}
