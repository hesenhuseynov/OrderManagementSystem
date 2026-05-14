using FluentValidation;

namespace OrderManagementSystem.Features.Orders.Create
{
    public class CreateOrderRequestValidator:AbstractValidator<CreateOrderRequest>
    {
        public CreateOrderRequestValidator()
        {
            RuleFor(x => x.CustomerId)
               .GreaterThan(0)
               .WithMessage("CustomerId must be greater than 0")
               .WithErrorCode("order.invalid_customer_id");

            RuleFor(x => x.Items)
               .NotEmpty()
               .WithMessage("Order must be container at least one item")
               .WithErrorCode("order.items_required");

            RuleForEach(x => x.Items)
                 .SetValidator(new CreateOrderItemRequestValidator()); 
        }
    }

    public sealed class CreateOrderItemRequestValidator:AbstractValidator<CreateOrderItemRequest>
    {
        public CreateOrderItemRequestValidator()
        {
            RuleFor(x => x.ProductId)
                 .GreaterThan(0)
                 .WithMessage("ProductId must bu greate than zero 0")
                 .WithErrorCode("order_item.invalid_product_id");

            RuleFor(x => x.Quantity)
                .GreaterThan(0)
                .WithMessage("Quantity must be greater than 0")
                .WithErrorCode("orde_item.invalid.quantity");
        }
    }
}
