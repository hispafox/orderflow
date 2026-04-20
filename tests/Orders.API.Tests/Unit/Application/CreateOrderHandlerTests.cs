using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Orders.API.Application.Commands;
using Orders.API.Application.Interfaces;
using Orders.API.Domain.Exceptions;
using Orders.API.Domain.Interfaces;
using Xunit;

namespace Orders.API.Tests.Unit.Application;

[Trait("Category", "Unit")]
public class CreateOrderHandlerTests
{
    private readonly IOrderRepository _repository = Substitute.For<IOrderRepository>();
    private readonly IEventPublisher  _publisher  = Substitute.For<IEventPublisher>();
    private readonly CreateOrderHandler _handler;

    public CreateOrderHandlerTests()
        => _handler = new CreateOrderHandler(
            _repository,
            _publisher,
            NullLogger<CreateOrderHandler>.Instance);

    [Fact]
    public async Task Handle_ValidCommand_CreatesOrderAndSavesToRepository()
    {
        var command = BuildValidCommand();

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.Status.Should().Be("Pending");
        result.CustomerId.Should().Be(command.CustomerId);
        result.Lines.Should().HaveCount(1);

        await _repository.Received(1).SaveAsync(
            Arg.Is<Orders.API.Domain.Entities.Order>(o => o.CustomerId == command.CustomerId),
            CancellationToken.None);
    }

    [Fact]
    public async Task Handle_ValidCommand_PublishesDomainEvent()
    {
        var command = BuildValidCommand();

        await _handler.Handle(command, CancellationToken.None);

        await _publisher.Received(1).PublishAsync(
            Arg.Any<Orders.API.Domain.Events.OrderCreatedEvent>(),
            CancellationToken.None);
    }

    [Fact]
    public async Task Handle_EmptyItems_ThrowsDomainException()
    {
        var command = new CreateOrderCommand(
            CustomerId:      Guid.NewGuid(),
            Items:           [],
            ShippingAddress: new("Gran Vía 28", "Madrid", "28013", "ES"));

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*at least one item*");

        await _repository.DidNotReceive().SaveAsync(
            Arg.Any<Orders.API.Domain.Entities.Order>(),
            Arg.Any<CancellationToken>());
    }

    private static CreateOrderCommand BuildValidCommand() =>
        new(
            CustomerId: Guid.NewGuid(),
            Items:
            [
                new(Guid.NewGuid(), "MacBook Pro", 1, 1999.99m, "EUR")
            ],
            ShippingAddress: new("Gran Vía 28", "Madrid", "28013", "ES"));
}
