using FluentValidation;

namespace Orders.API.Application.Commands;

/// <summary>
/// Validator de FluentValidation para CreateOrderCommand.
/// Se ejecuta automáticamente en el ValidationBehavior antes de llegar al handler.
/// Un validator por Command — nunca se mezclan reglas entre Commands.
/// </summary>
public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("CustomerId is required");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Order must have at least one item")
            .Must(items => items.Count <= 50)
            .WithMessage("Order cannot exceed 50 items");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId)
                .NotEmpty().WithMessage("ProductId is required");

            item.RuleFor(i => i.ProductName)
                .NotEmpty()
                .MaximumLength(200);

            item.RuleFor(i => i.Quantity)
                .GreaterThan(0)
                .LessThanOrEqualTo(1000);

            item.RuleFor(i => i.UnitPrice)
                .GreaterThan(0).WithMessage("Unit price must be positive");

            item.RuleFor(i => i.Currency)
                .NotEmpty()
                .Length(3).WithMessage("Currency must be a 3-letter ISO code");
        });

        RuleFor(x => x.ShippingAddress)
            .NotNull().WithMessage("Shipping address is required");

        When(x => x.ShippingAddress is not null, () =>
        {
            RuleFor(x => x.ShippingAddress.Street) .NotEmpty().MaximumLength(200);
            RuleFor(x => x.ShippingAddress.City)   .NotEmpty().MaximumLength(100);
            RuleFor(x => x.ShippingAddress.ZipCode).NotEmpty().MaximumLength(20);
            RuleFor(x => x.ShippingAddress.Country)
                .NotEmpty()
                .Length(2).WithMessage("Country must be a 2-letter ISO code");
        });
    }
}
