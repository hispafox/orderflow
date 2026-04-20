using Orders.API.Domain.Exceptions;

namespace Orders.API.Domain.ValueObjects;

public sealed record Address
{
    public string  Street  { get; }
    public string  City    { get; }
    public string  ZipCode { get; }
    public string  Country { get; }
    public string? State   { get; }

    public Address(
        string  street,
        string  city,
        string  zipCode,
        string  country,
        string? state = null)
    {
        Street  = Guard.NotNullOrEmpty(street,  nameof(street));
        City    = Guard.NotNullOrEmpty(city,    nameof(city));
        ZipCode = Guard.NotNullOrEmpty(zipCode, nameof(zipCode));
        Country = ValidateCountryCode(country);
        State   = state;
    }

    private static string ValidateCountryCode(string country)
    {
        if (string.IsNullOrWhiteSpace(country) || country.Length != 2)
            throw new DomainException($"Country must be a 2-letter ISO code. Got: '{country}'");
        return country.ToUpperInvariant();
    }

    public override string ToString() => $"{Street}, {City} {ZipCode}, {Country}";
}
