namespace OrderManagementSystem.Infrastructure.Search
{
    public sealed class ProductSearchDocument
    {
        public int ProductId { get; init; }

        public string Sku { get; init; } = string.Empty;

        public string ProductName { get; init; } = string.Empty;

        public decimal Price { get; init; }

        public bool IsActive { get; init; }
    }
}
