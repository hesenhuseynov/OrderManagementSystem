namespace OrderManagementSystem.Features.Orders.Create
{
    public sealed record CreateOrderRequest
    {
        public int CustomerId { get; init; }

        public IReadOnlyList<CreateOrderItemRequest> Items { get; init; }
            = Array.Empty<CreateOrderItemRequest>();
    }

    public  sealed record CreateOrderItemRequest
    {
        public int ProductId { get; init; }
        public int Quantity { get; init; }
    }


}
