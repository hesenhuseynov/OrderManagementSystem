namespace OrderManagementSystem.Features.Products.Search
{
    public record SearchProductResponse(int ProductId, string Sku, string ProductName, decimal Price, bool IsActive);
}
