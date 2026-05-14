namespace OrderManagementSystem.Features.Products.Create
{
    public record CreateProductResponse(int ProductId, string Sku, string ProductName, decimal Price, int StockQuantity, bool IsActive);
}
