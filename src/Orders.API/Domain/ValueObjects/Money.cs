using Orders.API.Domain.Exceptions;

namespace Orders.API.Domain.ValueObjects;

public sealed record Money
{
    public decimal Amount   { get; }
    public string  Currency { get; }

    public Money(decimal amount, string currency)
    {
        if (amount < 0)
            throw new DomainException($"Money amount cannot be negative. Got: {amount}");

        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
            throw new DomainException($"Currency must be a 3-letter ISO code. Got: '{currency}'");

        Amount   = amount;
        Currency = currency.ToUpperInvariant();
    }

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        var result = Amount - other.Amount;
        if (result < 0)
            throw new DomainException("Money cannot be negative after subtraction");
        return new Money(result, Currency);
    }

    public Money Multiply(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("Quantity must be positive");
        return new Money(Amount * quantity, Currency);
    }

    private void EnsureSameCurrency(Money other)
    {
        if (Currency != other.Currency)
            throw new DomainException(
                $"Cannot operate on different currencies: {Currency} and {other.Currency}");
    }

    public static Money Zero(string currency) => new(0, currency);

    public override string ToString() => $"{Amount:F2} {Currency}";
}
