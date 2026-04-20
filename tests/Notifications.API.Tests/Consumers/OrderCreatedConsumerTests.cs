using FluentAssertions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Notifications.API.Consumers;
using Notifications.API.Services;
using Notifications.API.Tests.Shared;
using NSubstitute;
using OrderFlow.Contracts.Events.Orders;
using Xunit;

namespace Notifications.API.Tests.Consumers;

[Trait("Category", "Integration")]
[Collection("NotificationsCollection")]
public class OrderCreatedConsumerTests
{
    private readonly NotificationsLocalDbFixture _fixture;

    public OrderCreatedConsumerTests(NotificationsLocalDbFixture fixture)
        => _fixture = fixture;

    private static OrderCreated BuildMessage(Guid? orderId = null) => new()
    {
        OrderId         = orderId ?? Guid.NewGuid(),
        CustomerId      = Guid.NewGuid(),
        CustomerEmail   = "john@techshop.es",
        Total           = 3999.99m,
        Currency        = "EUR",
        CreatedAt       = DateTime.UtcNow,
        Items           = [new(Guid.NewGuid(), "MacBook Pro", 1, 3999.99m)],
        ShippingAddress = new("Gran Vía 28", "Madrid", "28013", "ES")
    };

    private static ConsumeContext<OrderCreated> BuildContext(OrderCreated message)
    {
        var ctx = Substitute.For<ConsumeContext<OrderCreated>>();
        ctx.Message.Returns(message);
        ctx.CancellationToken.Returns(CancellationToken.None);
        return ctx;
    }

    [Fact]
    public async Task Consume_ValidOrderCreated_SendsConfirmationEmailAndRecordsNotification()
    {
        var emailService = Substitute.For<IEmailService>();
        await using var db = _fixture.CreateDbContext();
        var consumer = new OrderCreatedConsumer(
            emailService, db, NullLogger<OrderCreatedConsumer>.Instance);

        var message = BuildMessage();
        await consumer.Consume(BuildContext(message));

        await emailService.Received(1).SendOrderConfirmationAsync(
            Arg.Is<string>(e => e == "john@techshop.es"),
            Arg.Is<Guid>(id => id == message.OrderId),
            Arg.Any<decimal>(),
            Arg.Any<string>(),
            Arg.Any<IList<OrderCreatedItem>>(),
            Arg.Any<CancellationToken>());

        var notification = await db.Notifications
            .FirstOrDefaultAsync(n => n.OrderId == message.OrderId);
        notification.Should().NotBeNull();
        notification!.Type.Should().Be("OrderConfirmation");
    }

    [Fact]
    public async Task Consume_DuplicateOrderCreated_SkipsProcessing()
    {
        var emailService = Substitute.For<IEmailService>();
        await using var db = _fixture.CreateDbContext();
        var orderId = Guid.NewGuid();

        db.Notifications.Add(
            Notifications.API.Domain.Notification.Create(
                orderId, "OrderConfirmation", "john@techshop.es", true));
        await db.SaveChangesAsync();

        var consumer = new OrderCreatedConsumer(
            emailService, db, NullLogger<OrderCreatedConsumer>.Instance);

        await consumer.Consume(BuildContext(BuildMessage(orderId)));

        await emailService.DidNotReceive().SendOrderConfirmationAsync(
            Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<decimal>(),
            Arg.Any<string>(), Arg.Any<IList<OrderCreatedItem>>(),
            Arg.Any<CancellationToken>());
    }
}
