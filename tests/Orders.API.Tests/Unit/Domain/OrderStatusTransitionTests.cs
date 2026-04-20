using FluentAssertions;
using Orders.API.Domain.ValueObjects;

namespace Orders.API.Tests.Unit.Domain;

[Trait("Category", "Unit")]
public class OrderStatusTransitionTests
{
    [Theory]
    [InlineData("Pending",   "Confirmed", true)]
    [InlineData("Pending",   "Cancelled", true)]
    [InlineData("Confirmed", "Shipped",   true)]
    [InlineData("Confirmed", "Pending",   false)]
    [InlineData("Confirmed", "Cancelled", true)]
    [InlineData("Shipped",   "Delivered", true)]
    [InlineData("Delivered", "Cancelled", false)]
    [InlineData("Cancelled", "Pending",   false)]
    public void CanTransitionTo_ShouldRespectBusinessRules(
        string from, string to, bool expectedCanTransition)
    {
        var fromStatus = OrderStatus.FromString(from);
        var toStatus   = OrderStatus.FromString(to);

        var result = fromStatus.CanTransitionTo(toStatus);

        result.Should().Be(expectedCanTransition);
    }
}
