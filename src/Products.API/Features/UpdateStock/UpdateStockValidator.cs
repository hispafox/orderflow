using FluentValidation;

namespace Products.API.Features.UpdateStock;

public class UpdateStockValidator : AbstractValidator<UpdateStockRequest>
{
    public UpdateStockValidator()
    {
        RuleFor(x => x.NewStock)
            .GreaterThanOrEqualTo(0)   .WithMessage("Stock cannot be negative")
            .LessThanOrEqualTo(100_000).WithMessage("Stock exceeds maximum");
    }
}
