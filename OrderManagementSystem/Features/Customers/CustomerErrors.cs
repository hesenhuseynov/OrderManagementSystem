using OrderManagementSystem.Common.Errors;

namespace OrderManagementSystem.Features.Customers
{
    public static class CustomerErrors
    {

        //public static readonly Error Notfound = Error.NotFound(
        //    "customer.not_found",
        //    "The customer with the specified Id was not found"
        //    );

        //public static readonly Error InvalidId = Error.Validation(
        //    "customer.invalid_id",
        //    "Customer ID must be greater than zero"
        //    );

        public static Error NotFound(int id) =>
           Error.NotFound("customer.not_found", $"Customer with id  '{id} ' was not found");
    }
}
