using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using OrderFlow.Contracts.Events.Orders;
using Orders.API.Application.Commands;
using Orders.API.Domain.Interfaces;
using Orders.API.Infrastructure.Http;
using Xunit;

namespace Orders.API.Tests.Unit.Application;

[Trait("Category", "Unit")]
public class CreateOrderHandlerPublishTests
{
    private readonly IOrderRepository _repository      = Substitute.For<IOrderRepository>();
    private readonly IPublishEndpoint _publishEndpoint = Substitute.For<IPublishEndpoint>();

    private static ProductsClient BuildProductsClient(ProductDetailDto? product)
    {
        var client = Substitute.ForPartsOf<ProductsClient>(
            new System.Net.Http.HttpClient { BaseAddress = new Uri("http://test") },
            new MemoryCache(new MemoryCacheOptions()),
            NullLogger<ProductsClient>.Instance);
        client.GetProductAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
              .Returns(product);
        return client;
    }

    [Fact]
    public async Task Handle_ValidCommand_PublishesOrderCreatedEvent()
    {
        var productId = Guid.NewGuid();
        var product   = new ProductDetailDto(productId, "MacBook Pro", 3999.99m, "EUR", 25, true, Guid.Empty);
        var handler   = new CreateOrderHandler(
            _repository, BuildProductsClient(product),
            _publishEndpoint, NullLogger<CreateOrderHandler>.Instance);

        var command = new CreateOrderCommand(
            CustomerId:      Guid.NewGuid(),
            CustomerEmail:   "test@example.com",
            Items:           [new(productId, "MacBook Pro", 1, 3999.99m, "EUR")],
            ShippingAddress: new("Gran Vía 28", "Madrid", "28013", "ES"));

        var result = await handler.Handle(command, CancellationToken.None);

        result.Status.Should().Be("Pending");

        await _publishEndpoint.Received(1).Publish(
            Arg.Is<OrderCreated>(e =>
                e.CustomerEmail == "test@example.com" &&
                e.Total         == 3999.99m           &&
                e.Currency      == "EUR"              &&
                e.Items.Count   == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ProductNotAvailable_DoesNotPublishEvent()
    {
        var handler = new CreateOrderHandler(
            _repository, BuildProductsClient(null),
            _publishEndpoint, NullLogger<CreateOrderHandler>.Instance);

        var command = new CreateOrderCommand(
            Guid.NewGuid(), "test@example.com",
            [new(Guid.NewGuid(), "MacBook", 1, 3999.99m, "EUR")],
            new("c", "m", "28013", "ES"));

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<Orders.API.Domain.Exceptions.DomainException>();
        await _publishEndpoint.DidNotReceive().Publish(
            Arg.Any<OrderCreated>(), Arg.Any<CancellationToken>());
    }
}
