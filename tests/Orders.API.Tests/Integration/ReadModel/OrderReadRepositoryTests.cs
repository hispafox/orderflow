using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using OrderFlow.Contracts.Events.Orders;
using Orders.API.Application.Interfaces;
using Orders.API.Infrastructure.Consumers.Projectors;
using Orders.API.Infrastructure.Persistence;
using Orders.API.Infrastructure.Persistence.ReadModel;
using Orders.API.Tests.Shared.Fixtures;
using Xunit;

namespace Orders.API.Tests.Integration.ReadModel;

[Collection("OrdersApiCollection")]
[Trait("Category", "Integration")]
public class OrderReadRepositoryTests
{
    private readonly OrdersApiFactory _factory;

    public OrderReadRepositoryTests(OrdersApiFactory factory) => _factory = factory;

    private async Task SeedSummaryAsync(
        Guid?   orderId    = null,
        Guid?   customerId = null,
        string  status     = "Pending",
        decimal total      = 999.99m,
        string? city       = null)
    {
        using var scope = _factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        ctx.OrderSummaries.Add(new OrderSummary
        {
            OrderId       = orderId    ?? Guid.NewGuid(),
            CustomerId    = customerId ?? Guid.NewGuid(),
            CustomerEmail = "john@techshop.es",
            Status        = status,
            TotalAmount   = total,
            Currency      = "EUR",
            LinesCount    = 1,
            FirstItemName = "MacBook Pro",
            CreatedAt     = DateTime.UtcNow,
            ShippingCity  = city
        });
        await ctx.SaveChangesAsync();
    }

    private IOrderReadRepository GetRepo(out IServiceScope scope)
    {
        scope = _factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IOrderReadRepository>();
    }

    [Fact]
    public async Task ListAsync_FilterByStatus_ReturnsOnlyMatching()
    {
        var customerId = Guid.NewGuid();
        await SeedSummaryAsync(customerId: customerId, status: "Pending");
        await SeedSummaryAsync(customerId: customerId, status: "Confirmed");

        using var scope = _factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IOrderReadRepository>();

        var result = await repo.ListAsync(
            customerId: customerId, status: "Pending", page: 1, pageSize: 10);

        result.Items.Should().NotBeEmpty();
        result.Items.Should().AllSatisfy(o => o.Status.Should().Be("Pending"));
    }

    [Fact]
    public async Task ListAsync_FilterByCustomerId_ReturnsOnlyThatCustomer()
    {
        var target = Guid.NewGuid();
        var other  = Guid.NewGuid();
        await SeedSummaryAsync(customerId: target);
        await SeedSummaryAsync(customerId: other);

        using var scope = _factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IOrderReadRepository>();

        var result = await repo.ListAsync(
            customerId: target, status: null, page: 1, pageSize: 10);

        result.Items.Should().AllSatisfy(o => o.CustomerId.Should().Be(target));
    }

    [Fact]
    public async Task GetStatsByCustomerAsync_ReturnsCorrectAggregates()
    {
        var customerId = Guid.NewGuid();
        await SeedSummaryAsync(customerId: customerId, status: "Pending",   total: 100m);
        await SeedSummaryAsync(customerId: customerId, status: "Confirmed", total: 250m);
        await SeedSummaryAsync(customerId: customerId, status: "Confirmed", total: 400m);

        using var scope = _factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IOrderReadRepository>();

        var stats = await repo.GetStatsByCustomerAsync(customerId);

        stats.TotalOrders     .Should().Be(3);
        stats.PendingOrders   .Should().Be(1);
        stats.ConfirmedOrders .Should().Be(2);
        stats.TotalSpent      .Should().Be(750m);
        stats.LargestOrder    .Should().Be(400m);
    }

    [Fact]
    public async Task OrderSummaryProjector_DuplicateMessage_IsSkipped()
    {
        var orderId = Guid.NewGuid();
        var msg = new OrderCreated
        {
            OrderId       = orderId,
            CustomerId    = Guid.NewGuid(),
            CustomerEmail = "john@techshop.es",
            Total         = 999.99m,
            Currency      = "EUR",
            CreatedAt     = DateTime.UtcNow,
            Items         = [new OrderCreatedItem(Guid.NewGuid(), "MacBook Pro", 1, 999.99m)],
            ShippingAddress = new OrderCreatedAddress("Gran Vía 28", "Madrid", "28013", "ES")
        };

        using (var scope1 = _factory.Services.CreateScope())
        {
            var dbContext = scope1.ServiceProvider.GetRequiredService<OrderDbContext>();
            var projector = new OrderSummaryProjector(
                dbContext,
                scope1.ServiceProvider.GetRequiredService<ILogger<OrderSummaryProjector>>());
            var ctx = Substitute.For<ConsumeContext<OrderCreated>>();
            ctx.Message.Returns(msg);
            ctx.CancellationToken.Returns(CancellationToken.None);
            await projector.Consume(ctx);
        }

        using (var scope2 = _factory.Services.CreateScope())
        {
            var dbContext = scope2.ServiceProvider.GetRequiredService<OrderDbContext>();
            var projector = new OrderSummaryProjector(
                dbContext,
                scope2.ServiceProvider.GetRequiredService<ILogger<OrderSummaryProjector>>());
            var ctx = Substitute.For<ConsumeContext<OrderCreated>>();
            ctx.Message.Returns(msg);
            ctx.CancellationToken.Returns(CancellationToken.None);
            await projector.Consume(ctx);  // duplicate → should skip
        }

        using var scope3 = _factory.Services.CreateScope();
        var ctx3 = scope3.ServiceProvider.GetRequiredService<OrderDbContext>();
        var count = ctx3.OrderSummaries.Count(s => s.OrderId == orderId);
        count.Should().Be(1, "el proyector debe ser idempotente ante mensajes duplicados");
    }
}
