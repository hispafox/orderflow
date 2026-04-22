using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using OrderFlow.Contracts.Commands;
using OrderFlow.Contracts.Events.Orders;
using OrderFlow.Contracts.Events.Payments;
using OrderFlow.Contracts.Events.Products;
using Orders.API.Sagas;
using Xunit;

namespace Orders.API.Tests.Unit.Sagas;

[Trait("Category", "Unit")]
public class OrderSagaTests
{
    private static OrderCreated BuildOrderCreated(Guid? orderId = null) => new()
    {
        OrderId         = orderId ?? Guid.NewGuid(),
        CustomerId      = Guid.NewGuid(),
        CustomerEmail   = "test@techshop.es",
        Total           = 3999.99m,
        Currency        = "EUR",
        CreatedAt       = DateTime.UtcNow,
        Items           = [new(Guid.NewGuid(), "MacBook Pro", 1, 3999.99m)],
        ShippingAddress = new("Gran Vía 28", "Madrid", "28013", "ES")
    };

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddMassTransitTestHarness(x =>
            x.AddSagaStateMachine<OrderSaga, OrderSagaState>()
             .InMemoryRepository());
        return services.BuildServiceProvider(true);
    }

    [Fact]
    public async Task HappyPath_OrderCreated_To_Completed()
    {
        await using var provider = BuildServiceProvider();
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var orderId      = Guid.NewGuid();
        var orderCreated = BuildOrderCreated(orderId);

        // 1. OrderCreated → Saga publica ReserveStock
        await harness.Bus.Publish(orderCreated);
        (await harness.Published.Any<ReserveStock>()).Should().BeTrue();

        // 2. StockReserved → Saga publica ProcessPayment
        await harness.Bus.Publish(new StockReserved
        {
            OrderId          = orderId,
            ProductId        = orderCreated.Items[0].ProductId,
            ReservedQuantity = 1
        });
        (await harness.Published.Any<ProcessPayment>()).Should().BeTrue();

        // 3. PaymentProcessed → Saga publica OrderConfirmed
        await harness.Bus.Publish(new PaymentProcessed
        {
            OrderId     = orderId,
            PaymentId   = Guid.NewGuid(),
            CustomerId  = orderCreated.CustomerId,
            Amount      = 3999.99m,
            Currency    = "EUR",
            ProcessedAt = DateTime.UtcNow
        });

        (await harness.Published.Any<OrderConfirmed>()).Should().BeTrue();

        // Verificar contenido del evento
        var confirmed = harness.Published.Select<OrderConfirmed>()
            .FirstOrDefault(m => m.Context?.Message?.OrderId == orderId);
        confirmed.Should().NotBeNull();
        confirmed!.Context!.Message.CustomerEmail.Should().Be("test@techshop.es");
        confirmed!.Context!.Message.Total.Should().Be(3999.99m);

        await harness.Stop();
    }

    [Fact]
    public async Task CompensationPath_PaymentFailed_ReleasesStock_And_Fails()
    {
        await using var provider = BuildServiceProvider();
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var orderId      = Guid.NewGuid();
        var orderCreated = BuildOrderCreated(orderId);

        await harness.Bus.Publish(orderCreated);
        await harness.Published.Any<ReserveStock>();

        await harness.Bus.Publish(new StockReserved
        {
            OrderId          = orderId,
            ProductId        = orderCreated.Items[0].ProductId,
            ReservedQuantity = 1
        });
        await harness.Published.Any<ProcessPayment>();

        // PaymentFailed → Saga publica ReleaseStock
        await harness.Bus.Publish(new PaymentFailed
        {
            OrderId = orderId,
            Reason  = "Insufficient funds"
        });

        (await harness.Published.Any<ReleaseStock>()).Should().BeTrue();

        // StockReleased → Saga publica OrderFailed
        await harness.Bus.Publish(new StockReleased
        {
            OrderId          = orderId,
            ProductId        = orderCreated.Items[0].ProductId,
            ReleasedQuantity = 1
        });

        (await harness.Published.Any<OrderFailed>()).Should().BeTrue();

        // Verificar razón del fallo en el evento
        var failed = harness.Published.Select<OrderFailed>()
            .FirstOrDefault(m => m.Context?.Message?.OrderId == orderId);
        failed.Should().NotBeNull();
        failed!.Context!.Message.Reason.Should().Contain("Insufficient funds");

        await harness.Stop();
    }

    [Fact]
    public async Task DirectFail_StockInsufficient_PublishesOrderFailed()
    {
        await using var provider = BuildServiceProvider();
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var orderId      = Guid.NewGuid();
        var orderCreated = BuildOrderCreated(orderId);

        await harness.Bus.Publish(orderCreated);
        await harness.Published.Any<ReserveStock>();

        // Sin stock → OrderFailed directo sin pasar por cobro
        await harness.Bus.Publish(new StockInsufficient
        {
            OrderId           = orderId,
            ProductId         = orderCreated.Items[0].ProductId,
            RequestedQuantity = 1,
            AvailableQuantity = 0
        });

        (await harness.Published.Any<OrderFailed>()).Should().BeTrue();
        // NO debe publicar ProcessPayment
        (await harness.Published.Any<ProcessPayment>()).Should().BeFalse();

        await harness.Stop();
    }
}
