using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Orders.API.Application.Commands;
using Orders.API.Domain.Exceptions;
using Orders.API.Domain.Interfaces;
using Orders.API.Infrastructure.Http;
using Xunit;

namespace Orders.API.Tests.Unit.Application;

[Trait("Category", "Unit")]
public class CreateOrderHandlerTests
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
    public async Task Handle_ValidCommand_CreatesOrderAndSavesToRepository()
    {
        var productId = Guid.NewGuid();
        var product   = new ProductDetailDto(productId, "MacBook Pro", 1999.99m, "EUR", 10, true, Guid.Empty);
        var handler   = new CreateOrderHandler(
            _repository, BuildProductsClient(product),
            _publishEndpoint, NullLogger<CreateOrderHandler>.Instance);

        var command = BuildValidCommand(productId);
        var result  = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.Status.Should().Be("Pending");
        result.CustomerId.Should().Be(command.CustomerId);
        result.Lines.Should().HaveCount(1);

        await _repository.Received(1).SaveAsync(
            Arg.Is<Orders.API.Domain.Entities.Order>(o => o.CustomerId == command.CustomerId),
            CancellationToken.None);
    }

    [Fact]
    public async Task Handle_EmptyItems_ThrowsDomainException()
    {
        var handler = new CreateOrderHandler(
            _repository, BuildProductsClient(null),
            _publishEndpoint, NullLogger<CreateOrderHandler>.Instance);

        var command = new CreateOrderCommand(
            CustomerId:      Guid.NewGuid(),
            CustomerEmail:   "test@example.com",
            Items:           [],
            ShippingAddress: new("Gran Vía 28", "Madrid", "28013", "ES"));

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*at least one item*");

        await _repository.DidNotReceive().SaveAsync(
            Arg.Any<Orders.API.Domain.Entities.Order>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ProductNotFound_ThrowsDomainException()
    {
        var handler = new CreateOrderHandler(
            _repository, BuildProductsClient(null),
            _publishEndpoint, NullLogger<CreateOrderHandler>.Instance);

        var command = BuildValidCommand(Guid.NewGuid());

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*not available*");
    }

    private static CreateOrderCommand BuildValidCommand(Guid? productId = null) =>
        new(
            CustomerId:      Guid.NewGuid(),
            CustomerEmail:   "test@example.com",
            Items:           [new(productId ?? Guid.NewGuid(), "MacBook Pro", 1, 1999.99m, "EUR")],
            ShippingAddress: new("Gran Vía 28", "Madrid", "28013", "ES"));
}
