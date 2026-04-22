using FluentValidation;

namespace Orders.API.Application.Commands;

public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("CustomerId is required");

        RuleFor(x => x.CustomerEmail)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256)
            .Matches(@"^[^<>\r\n\t;]+$")
            .WithMessage("Invalid email format or characters");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Order must have at least one item")
            .Must(items => items.Count <= 50)
            .WithMessage("Maximum 50 items per order");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId)
                .NotEmpty();

            item.RuleFor(i => i.ProductName)
                .NotEmpty()
                .MaximumLength(200)
                .Matches(@"^[^<>""'&]+$")
                .WithMessage("ProductName contains invalid characters");

            item.RuleFor(i => i.Quantity)
                .GreaterThan(0).WithMessage("Quantity must be at least 1")
                .LessThanOrEqualTo(999).WithMessage("Maximum quantity is 999");

            item.RuleFor(i => i.UnitPrice)
                .GreaterThan(0.01m).WithMessage("UnitPrice must be greater than 0")
                .LessThanOrEqualTo(99_999.99m).WithMessage("UnitPrice exceeds maximum allowed");
        });

        RuleFor(x => x.ShippingAddress)
            .NotNull().WithMessage("Shipping address is required");

        When(x => x.ShippingAddress is not null, () =>
        {
            RuleFor(x => x.ShippingAddress.Street)
                .NotEmpty()
                .MaximumLength(200)
                .Matches(@"^[\w\s\.,\-À-ÿ]+$")
                .WithMessage("Street contains invalid characters");

            RuleFor(x => x.ShippingAddress.City)
                .NotEmpty()
                .MaximumLength(100)
                .Matches(@"^[\w\s\-À-ÿ]+$")
                .WithMessage("City contains invalid characters");

            RuleFor(x => x.ShippingAddress.ZipCode)
                .NotEmpty()
                .MaximumLength(20)
                .Matches(@"^[\w\s\-]+$")
                .WithMessage("ZipCode contains invalid characters");

            RuleFor(x => x.ShippingAddress.Country)
                .NotEmpty()
                .MaximumLength(2)
                .Matches(@"^[A-Z]{2}$")
                .WithMessage("Country must be a 2-letter ISO code (e.g. ES, DE)");
        });
    }
}
