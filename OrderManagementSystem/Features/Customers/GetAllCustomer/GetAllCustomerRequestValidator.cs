using FluentValidation;

namespace OrderManagementSystem.Features.Customers.GetAllCustomer
{
    public class GetAllCustomerRequestValidator:AbstractValidator<GetAllCustomerRequest>
    { 
        public GetAllCustomerRequestValidator()
        {
            RuleFor(x => x.PageNumber)
                .GreaterThan(0)
                .WithMessage("PageNumber  must be greater than 0 ")
                .WithErrorCode("pagination.invalid_page_number");


            RuleFor(x => x.PageSize)
                .GreaterThan(0)
                .WithMessage("PageSize must be between 1 and 100")
                .WithErrorCode("pagination.invalid_page_size");

            RuleFor(x => x.PageSize)
                .LessThanOrEqualTo(100)
                .WithMessage("PageSize cannot be greate than 100")
                .WithErrorCode("pagination.page_size_too_large"); 
        }
    }
}
