namespace OrderManagementSystem.Features.Orders.GetById
{
    public sealed record  GetOrderByIdResponse
    {
        public  int OrderId   { get; init; }
        public string OrderNumber { get; init; } = string.Empty;

        public int CustomerId  { get; init; }

        public string CustomerFullName { get; init; } = String.Empty;  

        public DateTime  OrderDate   { get; init; }

        public string Status { get; init; } = string.Empty;

        public string Currency { get; init; } = string.Empty;

        public decimal  TotalAmount  { get; init; }

        public IReadOnlyList<GetOrderByIdItemResponse> Items { get; init; } = [];  
    }

    public sealed record GetOrderByIdItemResponse
    {
        public int ProductId { get; init; }

        public string ProductName { get; init; } = string.Empty;
        public  int Quantity   { get; init; }

        public decimal UnitPrice  { get; init; }

        public  decimal LineTotal  { get; init; }
    }
}
