using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using OrderFlow.Contracts.Events.Orders;
using Orders.API.Tests.Shared;
using Orders.API.Tests.Shared.Fixtures;
using Xunit;

namespace Orders.API.Tests.Integration.Outbox;

[Collection("OrdersApiCollection")]
[Trait("Category", "Integration")]
public class OutboxPatternTests
{
    private readonly HttpClient      _client;
    private readonly OrdersApiFactory _factory;

    private static readonly object ValidOrderRequest = new
    {
        customerId      = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
        customerEmail   = "john@techshop.es",
        items           = new[]
        {
            new
            {
                productId   = Guid.NewGuid(),
                productName = "Test Product",
                quantity    = 1,
                unitPrice   = 99.99m,
                currency    = "EUR"
            }
        },
        shippingAddress = new
        {
            street  = "Gran Vía 28", city    = "Madrid",
            zipCode = "28013",       country = "ES"
        }
    };

    public OutboxPatternTests(OrdersApiFactory factory)
    {
        _factory = factory;
        _client  = factory.CreateClient();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTestHelper.CustomerToken());
    }

    [Fact]
    public async Task CreateOrder_PublishesOrderCreatedEvent()
    {
        var harness = _factory.Services.GetRequiredService<ITestHarness>();

        var response = await _client.PostAsJsonAsync("/api/orders", ValidOrderRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        (await harness.Published.Any<OrderCreated>()).Should().BeTrue(
            "OrderCreated event should be published after successful order creation");
    }

    [Fact]
    public async Task CreateOrder_OrderCreatedEvent_HasCorrectCustomerEmail()
    {
        var harness = _factory.Services.GetRequiredService<ITestHarness>();

        var response = await _client.PostAsJsonAsync("/api/orders", ValidOrderRequest);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var published = harness.Published.Select<OrderCreated>().LastOrDefault();
        published.Should().NotBeNull();

        if (published is null) return;
        published.Context!.Message.CustomerEmail.Should().Be("john@techshop.es");
        published.Context.Message.OrderId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateOrder_InvalidRequest_NoEventPublished()
    {
        var harness = _factory.Services.GetRequiredService<ITestHarness>();
        var countBefore = harness.Published.Select<OrderCreated>().Count();

        var invalidRequest = new
        {
            customerId    = Guid.NewGuid(),
            customerEmail = "not-valid-email",
            items         = Array.Empty<object>(),
            shippingAddress = new { street = "x", city = "x", zipCode = "x", country = "ES" }
        };

        var response = await _client.PostAsJsonAsync("/api/orders", invalidRequest);

        // [EmailAddress] model binding returns 400 before reaching FluentValidation
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity);
        harness.Published.Select<OrderCreated>().Count().Should().Be(countBefore,
            "no event should be published for invalid requests");
    }
}
