namespace OrderManagementSystem.Features.Customers.GetAllCustomer
{
    public class GetAllCustomersResponse
    {
        public  int CustomerId { get; init; }

        public string FirstName { get; init; } = string.Empty;

        public string LastName { get; init; } = string.Empty;
       
        public string Email { get; set; } = string.Empty;


    }
}
