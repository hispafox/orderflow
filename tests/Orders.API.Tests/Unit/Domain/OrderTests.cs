using FluentAssertions;
using Orders.API.Domain.Events;
using Orders.API.Domain.Exceptions;
using Orders.API.Domain.ValueObjects;
using Orders.API.Tests.Shared.Builders;

namespace Orders.API.Tests.Unit.Domain;

[Trait("Category", "Unit")]
public class OrderTests
{
    [Fact]
    public void Create_ValidOrder_ShouldHavePendingStatusAndRegisterEvent()
    {
        var order = new OrderBuilder()
            .WithLine("MacBook Pro", quantity: 1, price: 1999.99m)
            .Build();

        order.Status.Should().Be(OrderStatus.Pending);
        order.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        order.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<OrderCreatedEvent>();
    }

    [Fact]
    public void Create_EmptyItems_ShouldThrowDomainException()
    {
        var address = new Address("Calle Mayor 1", "Madrid", "28001", "ES");
        var act = () => Orders.API.Domain.Entities.Order.Create(Guid.NewGuid(), [], address);

        act.Should().Throw<DomainException>()
            .WithMessage("*at least one item*");
    }

    [Fact]
    public void Create_MoreThan50Lines_ShouldThrowDomainException()
    {
        var builder = new OrderBuilder().WithManyLines(51);
        var act = () => builder.Build();

        act.Should().Throw<DomainException>()
            .WithMessage("*50 lines*");
    }

    [Fact]
    public void Confirm_PendingOrder_ShouldChangeStatusAndRegisterEvent()
    {
        var order = new OrderBuilder().Build();

        order.Confirm();

        order.Status.Should().Be(OrderStatus.Confirmed);
        order.ConfirmedAt.Should().NotBeNull();
        order.DomainEvents.Should().HaveCount(2);
        order.DomainEvents.Last().Should().BeOfType<OrderConfirmedEvent>();
    }

    [Fact]
    public void Cancel_PendingOrder_ShouldWorkWithReason()
    {
        var order = new OrderBuilder().Build();

        order.Cancel("Customer changed their mind");

        order.Status.Should().Be(OrderStatus.Cancelled);
        order.CancellationReason.Should().Be("Customer changed their mind");
        order.CancelledAt.Should().NotBeNull();
    }

    [Fact]
    public void Cancel_DeliveredOrder_ShouldThrowDomainException()
    {
        var order = new OrderBuilder().BuildConfirmed();
        order.Ship();
        order.Deliver();

        var act = () => order.Cancel("Too late");

        act.Should().Throw<DomainException>()
            .WithMessage("*Cannot transition from 'Delivered'*");
    }

    [Fact]
    public void Total_ShouldBeCalculatedFromLines()
    {
        var order = new OrderBuilder()
            .WithLine("MacBook Pro", quantity: 1, price: 1999.99m)
            .WithLine("Magic Mouse", quantity: 2, price: 79.99m)
            .Build();

        order.Total.Amount.Should().Be(1999.99m + 79.99m * 2);
        order.Total.Currency.Should().Be("EUR");
    }

    [Fact]
    public void ClearDomainEvents_ShouldRemoveAllEvents()
    {
        var order = new OrderBuilder().Build();

        order.ClearDomainEvents();

        order.DomainEvents.Should().BeEmpty();
    }
}
