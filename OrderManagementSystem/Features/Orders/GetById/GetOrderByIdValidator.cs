using FluentValidation;

namespace OrderManagementSystem.Features.Orders.GetById
{
    public class GetOrderByIdValidator :AbstractValidator<GetOrderByIdRequest>
    {
        public GetOrderByIdValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("Id must be greater than 0")
                .WithErrorCode("order.invalid_id"); 
        }
    }
}
