using Orders.API.Domain.Exceptions;

namespace Orders.API.Domain;

public static class Guard
{
    public static string NotNullOrEmpty(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException($"{paramName} cannot be null or empty");
        return value;
    }

    public static T NotNull<T>(T? value, string paramName) where T : class
    {
        if (value is null)
            throw new DomainException($"{paramName} cannot be null");
        return value;
    }

    public static int Positive(int value, string paramName)
    {
        if (value <= 0)
            throw new DomainException($"{paramName} must be positive. Got: {value}");
        return value;
    }

    public static decimal NonNegative(decimal value, string paramName)
    {
        if (value < 0)
            throw new DomainException($"{paramName} cannot be negative. Got: {value}");
        return value;
    }
}
