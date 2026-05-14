namespace OrderManagementSystem.Features.Orders.Create
{
    public sealed record CreateOrderResponse(
       int OrderId,
       string OrderNumber,
       int CustomerId,
       DateTime OrderDate,
       string Status,
       string Currency,
       decimal TotalAmount);
}
