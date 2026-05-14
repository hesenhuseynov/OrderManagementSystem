using FluentValidation;

namespace OrderManagementSystem.Features.Customers.GetById
{
    public sealed class GetCustomerByIdValidator:AbstractValidator<GetCustomerByIdRequest>
    {
        public GetCustomerByIdValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("Id must be greaterh than 0")
                .WithErrorCode("customer.invalid_id");
        }
    }
}
