using FluentValidation;

namespace Products.API.Features.CreateProduct;

public class CreateProductValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()       .WithMessage("Product name is required")
            .MinimumLength(3) .WithMessage("Name must be at least 3 characters")
            .MaximumLength(200).WithMessage("Name cannot exceed 200 characters");

        RuleFor(x => x.Price)
            .GreaterThan(0)              .WithMessage("Price must be positive")
            .LessThanOrEqualTo(99_999.99m).WithMessage("Price exceeds maximum");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .Length(3).WithMessage("Currency must be a 3-letter ISO code")
            .Must(c => new[] { "EUR", "USD", "GBP" }.Contains(c.ToUpperInvariant()))
            .WithMessage("Currency must be EUR, USD or GBP");

        RuleFor(x => x.InitialStock)
            .GreaterThanOrEqualTo(0)   .WithMessage("Stock cannot be negative")
            .LessThanOrEqualTo(100_000).WithMessage("Stock exceeds maximum");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Category is required");
    }
}
