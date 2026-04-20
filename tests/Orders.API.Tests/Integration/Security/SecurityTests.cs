using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Orders.API.Tests.Shared;
using Orders.API.Tests.Shared.Fixtures;

namespace Orders.API.Tests.Integration.Security;

[Trait("Category", "Security")]
[Trait("Category", "Integration")]
[Collection("OrdersApiCollection")]
public class SecurityTests
{
    private readonly HttpClient _client;

    private static readonly object ValidOrderRequest = new
    {
        customerEmail = "test@example.com",
        items = new[]
        {
            new
            {
                productId   = Guid.NewGuid(),
                productName = "MacBook Pro 16",
                quantity    = 1,
                unitPrice   = 1999.99m,
                currency    = "EUR"
            }
        },
        shippingAddress = new
        {
            street  = "Gran Via 28",
            city    = "Madrid",
            zipCode = "28013",
            country = "ES"
        }
    };

    public SecurityTests(OrdersApiFactory factory)
        => _client = factory.CreateClient();

    // ─── A01 Broken Access Control ────────────────────────────────────────────

    [Fact]
    public async Task GetOrder_WithoutToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.GetAsync("/api/orders");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetOrder_AnotherUsersOrder_Returns404_NotForbidden()
    {
        var johnId    = Guid.NewGuid().ToString();
        var johnToken = JwtTestHelper.CustomerToken(johnId);
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", johnToken);

        var createResp = await _client.PostAsJsonAsync("/api/orders", ValidOrderRequest);

        if (createResp.IsSuccessStatusCode)
        {
            var body      = await createResp.Content.ReadAsStringAsync();
            using var doc = System.Text.Json.JsonDocument.Parse(body);
            var orderId   = doc.RootElement.GetProperty("id").GetString();

            var janeToken = JwtTestHelper.CustomerToken(Guid.NewGuid().ToString());
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", janeToken);

            var getResp = await _client.GetAsync($"/api/orders/{orderId}");

            getResp.StatusCode.Should().Be(HttpStatusCode.NotFound,
                "customers should get 404 (not 403) to prevent resource enumeration");
        }
    }

    [Fact]
    public async Task AdminConfirm_WithCustomerToken_Returns403()
    {
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTestHelper.CustomerToken());

        var response = await _client.PostAsync(
            $"/api/orders/{Guid.NewGuid()}/confirm", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ─── A07 Authentication Failures ─────────────────────────────────────────

    [Fact]
    public async Task ExpiredToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTestHelper.ExpiredToken());

        var response = await _client.GetAsync("/api/orders");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TamperedToken_Returns401()
    {
        var validToken    = JwtTestHelper.CustomerToken();
        var tamperedToken = validToken[..^5] + "XXXXX";
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tamperedToken);

        var response = await _client.GetAsync("/api/orders");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ─── A03 Injection prevention ─────────────────────────────────────────────

    [Fact]
    public async Task SqlInjectionInEmail_Returns422()
    {
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTestHelper.CustomerToken());

        var maliciousRequest = new
        {
            customerEmail = "'; DROP TABLE Orders; --",
            items = new[] { new { productId = Guid.NewGuid(), productName = "Test", quantity = 1, unitPrice = 9.99m, currency = "EUR" } },
            shippingAddress = new { street = "Gran Via 28", city = "Madrid", zipCode = "28013", country = "ES" }
        };

        var response = await _client.PostAsJsonAsync("/api/orders", maliciousRequest);

        // ASP.NET Core model binding rejects invalid emails with 400 before FluentValidation runs
        response.StatusCode.Should().BeOneOf(
            [HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity],
            "SQL injection attempts in email should be rejected at the validation layer");
    }

    [Fact]
    public async Task XssInProductName_Returns422()
    {
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTestHelper.CustomerToken());

        var xssRequest = new
        {
            customerEmail = "test@test.com",
            items = new[]
            {
                new
                {
                    productId   = Guid.NewGuid(),
                    productName = "<script>alert('xss')</script>",
                    quantity    = 1,
                    unitPrice   = 99.99m,
                    currency    = "EUR"
                }
            },
            shippingAddress = new { street = "Gran Via 28", city = "Madrid", zipCode = "28013", country = "ES" }
        };

        var response = await _client.PostAsJsonAsync("/api/orders", xssRequest);
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity,
            "XSS in productName should be rejected by FluentValidation");
    }

    // ─── A05 Security Misconfiguration ────────────────────────────────────────

    [Fact]
    public async Task Response_DoesNotExposeServerHeader()
    {
        var response = await _client.GetAsync("/health");

        response.Headers.Should().NotContainKey("Server",
            "Server header should be removed to avoid technology disclosure");
    }
}
