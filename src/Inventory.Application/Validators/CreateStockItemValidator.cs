using FluentValidation;

namespace Inventory.Application.Validators;

public class CreateStockItemValidator : AbstractValidator<CreateStockItemRequest>
{
    public CreateStockItemValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Product ID is required")
            .MaximumLength(100);

        RuleFor(x => x.ProductName)
            .NotEmpty().WithMessage("Product name is required")
            .MaximumLength(200);

        RuleFor(x => x.Quantity)
            .GreaterThanOrEqualTo(0).WithMessage("Quantity must be non-negative");
    }
}

public record CreateStockItemRequest(string ProductId, string ProductName, int Quantity);
