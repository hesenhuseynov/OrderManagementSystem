using FluentValidation;

namespace OrderManagementSystem.Features.Orders.Cancel
{
    public class CancelOrderRequestValidator:AbstractValidator<CancelOrderRequest>
    {
        public CancelOrderRequestValidator()
        {
            RuleFor(x => x.Id)
               .GreaterThan(0)
               .WithMessage("Id must be greater than")
               .WithErrorCode("cancelorder.invalid_id");
        }
    }
}
