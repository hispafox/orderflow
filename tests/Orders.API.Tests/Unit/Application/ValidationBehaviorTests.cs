using FluentAssertions;
using FluentValidation;
using Orders.API.API.DTOs.Responses;
using Orders.API.Application.Behaviors;
using Orders.API.Application.Commands;
using Xunit;

namespace Orders.API.Tests.Unit.Application;

[Trait("Category", "Unit")]
public class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_InvalidCommand_ThrowsValidationException()
    {
        var validators = new IValidator<CreateOrderCommand>[]
        {
            new CreateOrderCommandValidator()
        };
        var behavior = new ValidationBehavior<CreateOrderCommand, OrderDto>(validators);

        var invalidCommand = new CreateOrderCommand(
            CustomerId:      Guid.Empty,
            Items:           [],
            ShippingAddress: new("s", "c", "z", "INVALID"));

        var act = () => behavior.Handle(
            invalidCommand,
            _ => Task.FromResult(new OrderDto()),
            CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().Contain(e => e.PropertyName == "CustomerId");
        ex.Which.Errors.Should().Contain(e => e.PropertyName == "Items");
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsNextDelegate()
    {
        var validators = new IValidator<CreateOrderCommand>[]
        {
            new CreateOrderCommandValidator()
        };
        var behavior = new ValidationBehavior<CreateOrderCommand, OrderDto>(validators);

        var validCommand = new CreateOrderCommand(
            CustomerId:      Guid.NewGuid(),
            Items:           [new(Guid.NewGuid(), "MacBook Pro", 1, 1999.99m, "EUR")],
            ShippingAddress: new("Gran Vía 28", "Madrid", "28013", "ES"));

        var nextCalled = false;

        await behavior.Handle(
            validCommand,
            _ =>
            {
                nextCalled = true;
                return Task.FromResult(new OrderDto());
            },
            CancellationToken.None);

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_NoValidators_CallsNextDelegate()
    {
        var behavior = new ValidationBehavior<CreateOrderCommand, OrderDto>([]);
        var nextCalled = false;

        var command = new CreateOrderCommand(
            Guid.NewGuid(), [], new("s", "c", "z", "ES"));

        await behavior.Handle(
            command,
            _ =>
            {
                nextCalled = true;
                return Task.FromResult(new OrderDto());
            },
            CancellationToken.None);

        nextCalled.Should().BeTrue();
    }
}
