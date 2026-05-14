namespace OrderManagementSystem.Features.Customers.GetById
{
    public sealed record GetCustomerByIdResponse(
    int CustomerId,
    string FirstName,
    string LastName,
    string Email);
}
