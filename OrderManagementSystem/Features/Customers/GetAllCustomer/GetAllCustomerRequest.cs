namespace OrderManagementSystem.Features.Customers.GetAllCustomer
{
    public class GetAllCustomerRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
