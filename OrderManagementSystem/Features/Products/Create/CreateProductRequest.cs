namespace OrderManagementSystem.Features.Products.Create
{
    public record CreateProductRequest(string Sku, string ProductName, decimal Price, int StockQuantity);
}
