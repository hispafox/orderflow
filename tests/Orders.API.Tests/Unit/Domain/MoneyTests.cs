using FluentAssertions;
using Orders.API.Domain.Exceptions;
using Orders.API.Domain.ValueObjects;

namespace Orders.API.Tests.Unit.Domain;

[Trait("Category", "Unit")]
public class MoneyTests
{
    [Theory]
    [InlineData(100,    "EUR", 50,   "EUR", 150)]
    [InlineData(999.99, "EUR", 0.01, "EUR", 1000)]
    [InlineData(0,      "EUR", 0,    "EUR", 0)]
    public void Add_SameCurrency_ShouldReturnSum(
        decimal amount1, string currency,
        decimal amount2, string _, decimal expectedTotal)
    {
        var money1 = new Money(amount1, currency);
        var money2 = new Money(amount2, currency);

        var result = money1.Add(money2);

        result.Amount.Should().Be(expectedTotal);
        result.Currency.Should().Be(currency);
    }

    [Fact]
    public void Add_DifferentCurrencies_ShouldThrowDomainException()
    {
        var euros   = new Money(100, "EUR");
        var dollars = new Money(100, "USD");
        var act = () => euros.Add(dollars);

        act.Should().Throw<DomainException>()
            .WithMessage("*different currencies*");
    }

    [Fact]
    public void Create_NegativeAmount_ShouldThrowDomainException()
    {
        var act = () => new Money(-1, "EUR");
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void TwoMoneyWithSameValues_ShouldBeEqual()
    {
        var money1 = new Money(100, "EUR");
        var money2 = new Money(100, "EUR");

        money1.Should().Be(money2);
    }

    [Fact]
    public void Multiply_ShouldReturnCorrectAmount()
    {
        var money = new Money(10.00m, "EUR");

        var result = money.Multiply(3);

        result.Amount.Should().Be(30.00m);
    }
}
