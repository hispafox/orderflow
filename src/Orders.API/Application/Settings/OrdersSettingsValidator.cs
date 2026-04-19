using Microsoft.Extensions.Options;

namespace Orders.API.Application.Settings;

public class OrdersSettingsValidator : IValidateOptions<OrdersSettings>
{
    public ValidateOptionsResult Validate(string? name, OrdersSettings options)
    {
        var errors = new List<string>();

        if (!options.AllowBackorders && options.MinOrderAmount < 5.00m)
        {
            errors.Add(
                "Cuando AllowBackorders es false, " +
                "MinOrderAmount debe ser al menos 5.00");
        }

        if (options.RetentionDays < 90)
        {
            errors.Add(
                "RetentionDays debe ser al menos 90 días " +
                "por requisitos de compliance");
        }

        return errors.Count > 0
            ? ValidateOptionsResult.Fail(errors)
            : ValidateOptionsResult.Success;
    }
}
