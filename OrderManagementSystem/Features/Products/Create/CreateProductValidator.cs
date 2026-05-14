using FluentValidation;

namespace OrderManagementSystem.Features.Products.Create
{
    public class CreateProductValidator:AbstractValidator<CreateProductRequest>
    {
        public CreateProductValidator()
        {
            RuleLevelCascadeMode = CascadeMode.Stop;

            RuleFor(x => x.Sku)
                .NotEmpty()
                .WithMessage("Sku is required")
                .WithErrorCode("product.sku_required")
                .MaximumLength(100)
                .WithMessage("SKU must not exceed 100 characters .")
                .WithErrorCode("product.sku_too_long");

            RuleFor(x => x.ProductName)
                .NotEmpty()
                .WithMessage("Product name is required")
                .WithErrorCode("product.name_required")
                .MaximumLength(200)
                .WithMessage("Produt name must not exceed 200 characters")
                .WithErrorCode("product.name_too_long");

            RuleFor(x => x.Price)
                .GreaterThan(0)
                .WithMessage("Price must be greater than 0.")
                .WithErrorCode("product.price_must_be_positive");

            RuleFor(x => x.StockQuantity)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Stock quantity cannot be negative.")
                .WithErrorCode("product.stock_cannot_be_negative");
        }
    }
}
